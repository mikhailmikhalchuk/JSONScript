using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using JSONScript.Runtime;
using JSONScript.VM;
using JSONScript.Compiler.Diagnostics;

namespace JSONScript.Compiler
{
    public class Compiler
    {
        private readonly Dictionary<string, FunctionSignature> signatureTable = new();
        private HashSet<string> assignedLocals = new();
        private List<Value> constants = new();
        private List<Opcode> bytecode = new();

        // Track which names are params
        private HashSet<string> paramNames = new();
        private string currentNamespace = "";
        private string pathToFileComp = "";
        private Dictionary<string, int> elementLineNumbers = new();
        private readonly Dictionary<string, CompiledFunction> functionTable = new();

        private JSType? currentReturnType = null;

        private static readonly HashSet<string> ReservedWords =
        [
            "let", "return", "print", "if", "else", "then", "while",
            "call", "push", "set", "get", "and", "or", "eq", "gt", "lt",
            "add", "subtract", "multiply", "divide", "length", "string",
            "name", "true", "false", "null", "namespace", "function",
            "params", "locals", "body", "args", "at", "value", "type",
            "condition", "array"
        ];

        private DiagnosticBag diagnostics = new();

        public Dictionary<string, CompiledFunction> FunctionTable => functionTable;

        private int GetLineNumber(string path) => elementLineNumbers.TryGetValue(path, out int line) ? line : 0;

        private struct LocalInfo
        {
            public int Index;
            public JSType Type;
            public JSType? ElementType;

            public string? PointeeType;

            public LocalInfo(int index, JSType type, JSType? elementType = null, string? pointeeType = null)
            {
                Index = index;
                Type = type;
                ElementType = elementType;
                PointeeType = pointeeType;
            }
        }

        private Dictionary<string, LocalInfo> locals = new();

        public void CompileFile(string path)
        {
            pathToFileComp = Path.GetFullPath(path);

            string jsonText;
            try
            {
                jsonText = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                diagnostics.Report(pathToFileComp, $"Could not read file: {e.Message}");
                return;
            }

            try
            {
                TrackLineNumbers(jsonText);
            }
            catch (Exception e)
            {
                diagnostics.Report(pathToFileComp, $"Malformed JSON: {e.Message}");
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonText);
                CompileProgram(doc.RootElement);
            }
            catch (Exception e)
            {
                diagnostics.Report(pathToFileComp, $"Malformed JSON: {e.Message}");
                return;
            }
        }

        public void FlushDiagnostics()
        {
            if (diagnostics.HasErrors)
            {
                diagnostics.PrintAll();
                Environment.Exit(1);
            }
        }

        private void ResetFunctionState()
        {
            locals = new();
            assignedLocals = new();
            constants = new();
            bytecode = new();
            elementLineNumbers = new();
            currentReturnType = null;
            paramNames = new();
        }

        private void TrackLineNumbers(string jsonText)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonText);
            var reader = new Utf8JsonReader(bytes, isFinalBlock: true, state: default);

            var pathStack = new Stack<string>();
            var arrayIndexStack = new Stack<int>();
            string? pendingPropertyName = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                    {
                        pendingPropertyName = reader.GetString()!;
                        long offset = reader.TokenStartIndex;
                        int line = jsonText[..(int)offset].Count(c => c == '\n') + 1;
                        string fullPath = string.Join(".", pathStack.Reverse()) + (pathStack.Count > 0 ? "." : "") + pendingPropertyName;
                        fullPath = fullPath.Replace(".[", "[");
                        elementLineNumbers[fullPath] = line;
                        break;
                    }

                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                    {
                        string segment;
                        if (arrayIndexStack.Count > 0 && arrayIndexStack.Peek() >= 0)
                        {
                            int idx = arrayIndexStack.Peek();
                            segment = $"[{idx}]";
                            int top = arrayIndexStack.Pop();
                            arrayIndexStack.Push(top + 1);
                        }
                        else if (pendingPropertyName != null)
                        {
                            segment = pendingPropertyName;
                            pendingPropertyName = null;
                        }
                        else
                        {
                            segment = "";
                        }

                        if (segment != "")
                            pathStack.Push(segment);

                        arrayIndexStack.Push(reader.TokenType == JsonTokenType.StartArray ? 0 : -1);
                        break;
                    }

                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        arrayIndexStack.Pop();

                        if (pathStack.Count > 0)
                            pathStack.Pop();
                            
                        break;

                    default:
                    {
                        string segment;
                        if (arrayIndexStack.Count > 0 && arrayIndexStack.Peek() >= 0)
                        {
                            int idx = arrayIndexStack.Peek();
                            segment = $"[{idx}]";
                            int top = arrayIndexStack.Pop();
                            arrayIndexStack.Push(top + 1);
                        }
                        else if (pendingPropertyName != null)
                        {
                            segment = pendingPropertyName;
                            pendingPropertyName = null;
                        }
                        else
                        {
                            break;
                        }

                        long offset = reader.TokenStartIndex;
                        int line = jsonText[..(int)offset].Count(c => c == '\n') + 1;
                        string fullPath = string.Join(".", pathStack.Reverse()) + (pathStack.Count > 0 ? "." : "") + segment;
                        fullPath = fullPath.Replace(".[", "[");
                        elementLineNumbers[fullPath] = line;
                        break;
                    }
                }
            }
        }

        private void CompileProgram(JsonElement root)
        {
            currentNamespace = root.TryGetProperty("namespace", out var ns) ? ns.GetString() ?? "Global" : "Global";

            if (!root.TryGetProperty("functions", out var functions))
            {
                diagnostics.Report(pathToFileComp, "JSON must contain 'functions' array");
                return;
            }

            int funcIndex = 0;
            foreach (var func in functions.EnumerateArray())
            {
                ResetFunctionState();
                TrackLineNumbers(File.ReadAllText(pathToFileComp));
                CompileFunction(func, $"functions[{funcIndex}]");
                funcIndex++;
            }
        }

        private void CompileFunction(JsonElement func, string path)
        {
            string funcName = func.GetProperty("name").GetString()!;
            string fullName = $"{currentNamespace}.{funcName}";

            if (funcName == "main")
            {
                if (func.TryGetProperty("type", out var typeToShow))
                {
                    diagnostics.Report(pathToFileComp, $"Expected 'main' to be of type void but got '{typeToShow}' at line {GetLineNumber(path + ".type")}");
                    return;
                }

                if (func.TryGetProperty("params", out var mainParams))
                {
                    var paramList = mainParams.EnumerateArray().ToList();
                    if (paramList.Count != 1)
                    {
                        diagnostics.Report(pathToFileComp, $"Expected 1 parameter but got {paramList.Count} at line {GetLineNumber(path + ".name")}");
                        return;
                    }
                    else
                    {
                        var first = paramList[0];
                        string? pType = first.TryGetProperty("type", out var t) ? t.GetString() : null;
                        if (pType != "string[]")
                        {
                            diagnostics.Report(pathToFileComp, $"Expected parameter to be of type string[] but got '{pType}' at line {GetLineNumber(path + ".name")}");
                            return;
                        }
                    }
                    
                }
            }

            if (functionTable.ContainsKey(fullName))
            {
                diagnostics.Report(pathToFileComp, $"Duplicate function '{fullName}'");
                return;
            }

            int paramCount = 0;
            if (func.TryGetProperty("params", out var paramsEl))
            {
                int paramIndex = 0;
                foreach (var param in paramsEl.EnumerateArray())
                {
                    string paramName = param.GetProperty("name").GetString()!;

                    if (ReservedWords.Contains(paramName))
                    {
                        diagnostics.Report(pathToFileComp, $"'{paramName}' is a reserved keyword and cannot be used as a parameter name at line {GetLineNumber(path + ".params[" + paramIndex + "].name")}");
                        paramIndex++;
                        continue;
                    }
                    paramIndex++;

                    string typeStr = param.TryGetProperty("type", out var t) ? t.GetString() ?? "int" : "int";
                    var (baseType, elementType, pointeeType) = ParseType(typeStr);
                    locals[paramName] = new LocalInfo(locals.Count, baseType, elementType, pointeeType);
                    assignedLocals.Add(paramName);
                    paramNames.Add(paramName);
                    paramCount++;
                }
            }

            if (func.TryGetProperty("locals", out var localsEl))
            {
                int localsIndex = 0;
                foreach (var loc in localsEl.EnumerateArray())
                {
                    string name = loc.GetProperty("name").GetString()!;
                    string currentPath = path + ".locals[" + localsIndex + "].name";
                    localsIndex++;

                    if (ReservedWords.Contains(name))
                    {
                        diagnostics.Report(pathToFileComp, $"'{name}' is a reserved keyword and cannot be used as a variable name at line {GetLineNumber(currentPath)}");
                        continue;
                    }

                    if (locals.ContainsKey(name))
                    {
                        diagnostics.Report(pathToFileComp, $"'{name}' is already declared as a parameter and cannot be redeclared as a local at line {GetLineNumber(currentPath)}");
                        continue;
                    }

                    string typeStr = loc.GetProperty("type").GetString() ?? "int";
                    var (baseType, elementType, pointeeType) = ParseType(typeStr);
                    locals[name] = new LocalInfo(locals.Count, baseType, elementType, pointeeType);
                }
            }

            if (!func.TryGetProperty("body", out var body))
            {
                diagnostics.Report(pathToFileComp, $"Function '{fullName}' is missing a body");
                return;
            }

            currentReturnType = null;
            if (func.TryGetProperty("type", out var retTypeProp))
            {
                string retTypeStr = retTypeProp.GetString()!;
                var (baseType, _, _) = ParseType(retTypeStr);
                currentReturnType = baseType;
            }

            int stmtIndex = 0;
            foreach (var stmt in body.EnumerateArray())
            {
                string stmtPath = $"{path}.body[{stmtIndex++}]";
                CompileStatement(stmt, stmtPath);
            }

            bytecode.Add(Opcode.HALT);

            functionTable[fullName] = new CompiledFunction(fullName, bytecode.Select(o => (byte)o).ToArray(), constants.ToArray(), locals.Count, paramCount);
        }

        private void CompileStatement(JsonElement stmt, string path)
        {
            if (stmt.TryGetProperty("let", out var letStmt))
            {
                foreach (var prop in letStmt.EnumerateObject())
                {
                    if (prop.Name != "name" && prop.Name != "type" && prop.Name != "value")
                    {
                        int ln = GetLineNumber(path + ".let." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in let statement at line {ln}");
                        return;
                    }
                }

                string name = letStmt.GetProperty("name").GetString()!;

                if (paramNames.Contains(name))
                {
                    diagnostics.Report(pathToFileComp, $"'{name}' is a parameter and cannot be reassigned at line {GetLineNumber(path + ".let.name")}");
                    return;
                }

                if (!locals.TryGetValue(name, out var localInfo))
                {
                    diagnostics.Report(pathToFileComp, $"Undefined variable '{name}' at line {GetLineNumber(path + ".let.name")}");
                    return;
                }

                if (!letStmt.TryGetProperty("value", out var valueExpr))
                {
                    diagnostics.Report(pathToFileComp, $"let statement for '{name}' is missing a value at line {GetLineNumber(path + ".let")}");
                    return;
                }

                if (valueExpr.ValueKind == JsonValueKind.Null && localInfo.Type == JSType.POINTER)
                {
                    int ci = AddConstant(new Value(0, localInfo.PointeeType ?? "void"));
                    Emit3(Opcode.PUSH_CONST, ci);
                    bytecode.Add(Opcode.STORE_LOCAL);
                    bytecode.Add((Opcode)(byte)localInfo.Index);
                    bytecode.Add((Opcode)(byte)(localInfo.Index >> 8));
                    assignedLocals.Add(name);
                    return; // skip the normal CompileExpression path
                }

                // Type check for non-array types
                if (localInfo.Type != JSType.ARRAY)
                {
                    JSType? inferredType = InferType(valueExpr);
                    if (inferredType.HasValue && !TypesCompatible(localInfo.Type, inferredType.Value, localInfo.PointeeType, null))
                    {
                        int line = GetLineNumber(path + ".let.name");
                        diagnostics.Report(pathToFileComp, $"Type mismatch: '{name}' is declared as '{localInfo.Type}' but assigned '{inferredType.Value}' at line {line}");
                        return;
                    }
                }

                // Array literal type check
                if (localInfo.Type == JSType.ARRAY && valueExpr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in valueExpr.EnumerateArray())
                    {
                        JSType? elType = InferType(element);
                        if (elType.HasValue && localInfo.ElementType.HasValue && !TypesCompatible(localInfo.ElementType.Value, elType.Value))
                        {
                            int line = GetLineNumber(path + ".let.value");
                            diagnostics.Report(pathToFileComp, $"Array '{name}' holds '{localInfo.ElementType}' but element is '{elType.Value}' at line {line}");
                            return;
                        }
                    }
                }

                CompileExpression(valueExpr, path + ".let.value");

                if (localInfo.Type == JSType.INT)
                {
                    bytecode.Add(Opcode.STORE_LOCAL_INT);
                }
                else
                {
                    bytecode.Add(Opcode.STORE_LOCAL);
                }

                bytecode.Add((Opcode)(byte)localInfo.Index);
                bytecode.Add((Opcode)(byte)(localInfo.Index >> 8));
                assignedLocals.Add(name);
            }
            else if (stmt.TryGetProperty("print", out var printStmt))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "print")
                    {
                        int line = GetLineNumber(path + "." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in print statement at line {line}");
                        return;
                    }
                }

                CompileExpression(printStmt, path + ".print");
                bytecode.Add(Opcode.PRINT);
            }
            else if (stmt.TryGetProperty("call", out var callStmt))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "call")
                    {
                        int line = GetLineNumber(path + "." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in call statement at line {line}");
                        return;
                    }
                }

                foreach (var prop in callStmt.EnumerateObject())
                {
                    if (prop.Name != "namespace" && prop.Name != "function" && prop.Name != "args")
                    {
                        int line = GetLineNumber(path + ".call." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in call at line {line}");
                        return;
                    }
                }

                CompileCall(callStmt, path + ".call", expectsValue: false);
            }
            else if (stmt.TryGetProperty("if", out var ifStmt))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "if")
                    {
                        int line = GetLineNumber(path + "." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in if statement at line {line}");
                        return;
                    }
                }

                foreach (var prop in ifStmt.EnumerateObject())
                {
                    if (prop.Name != "condition" && prop.Name != "then" && prop.Name != "else")
                    {
                        int line = GetLineNumber(path + ".if." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in if at line {line}");
                        return;
                    }
                }

                CompileIf(ifStmt, path + ".if");
            }
            else if (stmt.TryGetProperty("while", out var whileStmt))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "while")
                    {
                        int line = GetLineNumber(path + "." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in while statement at line {line}");
                        return;
                    }
                }

                foreach (var prop in whileStmt.EnumerateObject())
                {
                    if (prop.Name != "condition" && prop.Name != "body")
                    {
                        int line = GetLineNumber(path + ".while." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in while at line {line}");
                        return;
                    }
                }

                CompileWhile(whileStmt, path + ".while");
            }
            else if (stmt.TryGetProperty("set", out var setStmt))
            {
                foreach (var prop in setStmt.EnumerateObject())
                {
                    if (prop.Name != "array" && prop.Name != "at" && prop.Name != "value")
                    {
                        int line = GetLineNumber(path + ".set." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in set statement at line {line}");
                        return;
                    }
                }

                string arrName = setStmt.GetProperty("array").GetString()!;
                if (!locals.TryGetValue(arrName, out var arrInfo))
                {
                    diagnostics.Report(pathToFileComp, $"Undefined variable '{arrName}' at line {GetLineNumber(path + ".set.array")}");
                    return;
                }

                var valueEl = setStmt.GetProperty("value");
                JSType? inferredType = InferType(valueEl);
                if (inferredType.HasValue && arrInfo.ElementType.HasValue && !TypesCompatible(arrInfo.ElementType.Value, inferredType.Value))
                {
                    diagnostics.Report(pathToFileComp, $"Type mismatch: array '{arrName}' holds '{arrInfo.ElementType}' but assigned '{inferredType.Value}' at line {GetLineNumber(path + ".set.value")}");
                    return;
                }

                Emit3(Opcode.LOAD_LOCAL, arrInfo.Index);
                CompileExpression(setStmt.GetProperty("at"), path + ".set.at");
                CompileExpression(valueEl, path + ".set.value");
                bytecode.Add(Opcode.ARRAY_SET);
            }
            else if (stmt.TryGetProperty("push", out var pushStmt))
            {
                foreach (var prop in pushStmt.EnumerateObject())
                {
                    if (prop.Name != "array" && prop.Name != "value")
                    {
                        int line = GetLineNumber(path + ".push." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in push statement at line {line}");
                        return;
                    }
                }

                string arrName = pushStmt.GetProperty("array").GetString()!;
                if (!locals.TryGetValue(arrName, out var arrInfo))
                {
                    diagnostics.Report(pathToFileComp, $"Undefined variable '{arrName}' at line {GetLineNumber(path + ".push.array")}");
                    return;
                }

                var valueEl = pushStmt.GetProperty("value");
                JSType? inferredType = InferType(valueEl);
                if (inferredType.HasValue && arrInfo.ElementType.HasValue && !TypesCompatible(arrInfo.ElementType.Value, inferredType.Value))
                {
                    diagnostics.Report(pathToFileComp, $"Type mismatch: array '{arrName}' holds '{arrInfo.ElementType}' but pushing '{inferredType.Value}' at line {GetLineNumber(path + ".push.value")}");
                    return;
                }

                Emit3(Opcode.LOAD_LOCAL, arrInfo.Index);
                CompileExpression(valueEl, path + ".push.value");
                bytecode.Add(Opcode.ARRAY_PUSH);
            }
            else if (stmt.TryGetProperty("return", out var retExpr))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "return")
                    {
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in return statement at line {GetLineNumber(path + ".return")}");
                        return;
                    }
                }

                // Void function returning a value
                if (currentReturnType == null)
                {
                    diagnostics.Report(pathToFileComp, $"Function is void but contains a return value at line {GetLineNumber(path + ".return")}");
                    return;
                }

                // Type check return value
                JSType? inferredType = InferType(retExpr);
                if (inferredType.HasValue && !TypesCompatible(currentReturnType.Value, inferredType.Value))
                {
                    diagnostics.Report(pathToFileComp, $"Expected type '{currentReturnType}' but got '{inferredType.Value}' at line {GetLineNumber(path + ".return")}");
                    return;
                }

                CompileExpression(retExpr, path + ".return");
                bytecode.Add(Opcode.RET);
            }
            else if (stmt.TryGetProperty("on", out var onStmt))
            {
                foreach (var prop in onStmt.EnumerateObject())
                {
                    if (prop.Name != "event" && prop.Name != "call")
                    {
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in on statement at line {GetLineNumber(path + ".on." + prop.Name)}");
                        return;
                    }
                }

                if (!onStmt.TryGetProperty("event", out var eventProp))
                {
                    diagnostics.Report(pathToFileComp, $"'on' is missing 'event' at line {GetLineNumber(path + ".on")}");
                    return;
                }

                if (!onStmt.TryGetProperty("call", out var callProp))
                {
                    diagnostics.Report(pathToFileComp, $"'on' is missing 'call' at line {GetLineNumber(path + ".on")}");
                    return;
                }

                if (!callProp.TryGetProperty("namespace", out var nsProp) ||
                    !callProp.TryGetProperty("function", out var fnProp))
                {
                    diagnostics.Report(pathToFileComp, $"'on.call' requires 'namespace' and 'function' at line {GetLineNumber(path + ".on.call")}");
                    return;
                }

                string eventName = eventProp.GetString()!;
                string fullName  = $"{nsProp.GetString()}.{fnProp.GetString()}";

                // Push event name and function name as constants
                int eventIndex = AddConstant(new Value(eventName));
                int funcIndex  = AddConstant(new Value(fullName));
                Emit3(Opcode.PUSH_CONST, eventIndex);
                Emit3(Opcode.PUSH_CONST, funcIndex);
                bytecode.Add(Opcode.ON_EVENT);
            }
            else if (stmt.TryGetProperty("ffi", out var ffiStmt))
            {
                foreach (var prop in ffiStmt.EnumerateObject())
                {
                    if (prop.Name != "lib" && prop.Name != "symbol" && prop.Name != "args" && prop.Name != "returns" && prop.Name != "into")
                    {
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in ffi at line {GetLineNumber(path + ".ffi." + prop.Name)}");
                        return;
                    }
                }

                if (!ffiStmt.TryGetProperty("lib", out var libProp))
                {
                    diagnostics.Report(pathToFileComp, $"'ffi' is missing 'lib' at line {GetLineNumber(path + ".ffi")}");
                    return;
                }

                if (!ffiStmt.TryGetProperty("symbol", out var symbolProp))
                {
                    diagnostics.Report(pathToFileComp, $"'ffi' is missing 'symbol' at line {GetLineNumber(path + ".ffi")}");
                    return;
                }

                string lib    = libProp.GetString()!;
                string symbol = symbolProp.GetString()!;

                // Collect args
                int argCount = 0;
                if (ffiStmt.TryGetProperty("args", out var ffiArgs))
                {
                    foreach (var arg in ffiArgs.EnumerateArray())
                    {
                        if (!arg.TryGetProperty("value", out var argVal) || !arg.TryGetProperty("type", out var argTypeProp))
                        {
                            diagnostics.Report(pathToFileComp, $"ffi arg must have 'value' and 'type' at line {GetLineNumber(path + ".ffi.args")}");
                            return;
                        }

                        // Push type string as constant
                        int typeIdx = AddConstant(new Value(argTypeProp.GetString()!));
                        Emit3(Opcode.PUSH_CONST, typeIdx);

                        // Push value
                        CompileExpression(argVal, path + $".ffi.args[{argCount}].value");
                        argCount++;
                    }
                }

                // Push return type
                string returnType = ffiStmt.TryGetProperty("returns", out var retProp) ? retProp.GetString()! : "void";
                int retTypeIdx = AddConstant(new Value(returnType));
                Emit3(Opcode.PUSH_CONST, retTypeIdx);

                // Push lib and symbol
                int libIdx = AddConstant(new Value(lib));
                int symbolIdx = AddConstant(new Value(symbol));
                Emit3(Opcode.PUSH_CONST, libIdx);
                Emit3(Opcode.PUSH_CONST, symbolIdx);

                bytecode.Add(Opcode.FFI_CALL);
                bytecode.Add((Opcode)(byte)argCount);

                // Store result if "into" is specified
                if (ffiStmt.TryGetProperty("into", out var intoProp))
                {
                    string intoName = intoProp.GetString()!;
                    if (!locals.TryGetValue(intoName, out var intoInfo))
                    {
                        diagnostics.Report(pathToFileComp, $"Undefined variable '{intoName}' at line {GetLineNumber(path + ".ffi.into")}");
                        return;
                    }
                    bytecode.Add(Opcode.STORE_LOCAL);
                    bytecode.Add((Opcode)(byte)intoInfo.Index);
                    bytecode.Add((Opcode)(byte)(intoInfo.Index >> 8));
                    assignedLocals.Add(intoName);
                }
            }
            else if (stmt.TryGetProperty("alloc", out var allocStmt))
            {
                if (!allocStmt.TryGetProperty("size", out var sizeProp))
                {
                    diagnostics.Report(pathToFileComp, $"'alloc' missing 'size' at line {GetLineNumber(path + ".alloc")}");
                    return;
                }
                if (!allocStmt.TryGetProperty("into", out var intoProp))
                {
                    diagnostics.Report(pathToFileComp, $"'alloc' missing 'into' at line {GetLineNumber(path + ".alloc")}");
                    return;
                }

                string intoName = intoProp.GetString()!;
                if (!locals.TryGetValue(intoName, out var intoInfo))
                {
                    diagnostics.Report(pathToFileComp, $"Undefined variable '{intoName}' at line {GetLineNumber(path + ".alloc.into")}");
                    return;
                }

                CompileExpression(sizeProp, path + ".alloc.size");
                bytecode.Add(Opcode.MEM_ALLOC);
                bytecode.Add(Opcode.STORE_LOCAL);
                bytecode.Add((Opcode)(byte)intoInfo.Index);
                bytecode.Add((Opcode)(byte)(intoInfo.Index >> 8));
                assignedLocals.Add(intoName);
            }
            else if (stmt.TryGetProperty("memwrite", out var memWriteStmt))
            {
                if (!memWriteStmt.TryGetProperty("ptr", out var ptrProp) || !memWriteStmt.TryGetProperty("offset", out var offsetProp) || !memWriteStmt.TryGetProperty("value", out var valueProp) || !memWriteStmt.TryGetProperty("type", out var typeProp))
                {
                    diagnostics.Report(pathToFileComp, $"'memwrite' requires 'ptr', 'offset', 'value', 'type' at line {GetLineNumber(path + ".memwrite")}");
                    return;
                }

                CompileExpression(ptrProp,    path + ".memwrite.ptr");
                CompileExpression(offsetProp, path + ".memwrite.offset");
                CompileExpression(valueProp,  path + ".memwrite.value");
                int typeIdx = AddConstant(new Value(typeProp.GetString()!));
                Emit3(Opcode.PUSH_CONST, typeIdx);
                bytecode.Add(Opcode.MEM_WRITE);
            }
            else if (stmt.TryGetProperty("memread", out var memReadStmt))
            {
                if (!memReadStmt.TryGetProperty("ptr", out var ptrProp) || !memReadStmt.TryGetProperty("offset", out var offsetProp) || !memReadStmt.TryGetProperty("type", out var typeProp) || !memReadStmt.TryGetProperty("into", out var intoProp))
                {
                    diagnostics.Report(pathToFileComp, $"'memread' requires 'ptr', 'offset', 'type', 'into' at line {GetLineNumber(path + ".memread")}");
                    return;
                }

                string intoName = intoProp.GetString()!;
                if (!locals.TryGetValue(intoName, out var intoInfo))
                {
                    diagnostics.Report(pathToFileComp, $"Undefined variable '{intoName}' at line {GetLineNumber(path + ".memread.into")}");
                    return;
                }

                CompileExpression(ptrProp,    path + ".memread.ptr");
                CompileExpression(offsetProp, path + ".memread.offset");
                int typeIdx = AddConstant(new Value(typeProp.GetString()!));
                Emit3(Opcode.PUSH_CONST, typeIdx);
                bytecode.Add(Opcode.MEM_READ);
                bytecode.Add(Opcode.STORE_LOCAL);
                bytecode.Add((Opcode)(byte)intoInfo.Index);
                bytecode.Add((Opcode)(byte)(intoInfo.Index >> 8));
                assignedLocals.Add(intoName);
            }
            else if (stmt.TryGetProperty("free", out var freeStmt))
            {
                CompileExpression(freeStmt, path + ".free");
                bytecode.Add(Opcode.MEM_FREE);
            }
            else
            {
                string firstKey = stmt.EnumerateObject().FirstOrDefault().Name ?? "(no keys)";
                int line = GetLineNumber(path + "." + firstKey);
                diagnostics.Report(pathToFileComp, $"Unsupported statement kind '{firstKey}' at line {line}");
            }
        }

        private void CompileCall(JsonElement callExpr, string path, bool expectsValue = false)
        {
            if (!callExpr.TryGetProperty("namespace", out var nsProp) || !callExpr.TryGetProperty("function", out var funcProp))
            {
                int line = GetLineNumber(path);
                diagnostics.Report(pathToFileComp, $"Malformed call at line {line}, missing 'namespace' and/or 'function'");
                return;
            }

            string fullName = $"{nsProp.GetString()}.{funcProp.GetString()}";

            if (!signatureTable.TryGetValue(fullName, out var sig))
            {
                int line = GetLineNumber(path);
                diagnostics.Report(pathToFileComp, $"Unknown function '{fullName}' at line {line}");
                return;
            }

            var namedArgs = new Dictionary<string, JsonElement>();
            if (callExpr.TryGetProperty("args", out var args))
            {
                if (args.ValueKind != JsonValueKind.Object)
                {
                    int line = GetLineNumber(path + ".args");
                    diagnostics.Report(pathToFileComp, $"'args' must be an object with named arguments at line {line}");
                    return;
                }
                foreach (var arg in args.EnumerateObject())
                    namedArgs[arg.Name] = arg.Value;
            }

            int argCount = 0;
            foreach (var param in sig.Params)
            {
                if (namedArgs.TryGetValue(param.Name, out var argVal))
                {
                    CompileExpression(argVal, path + $".args.{param.Name}");
                    argCount++;
                }
                else if (param.HasDefault)
                {
                    int ci = AddConstant(param.Default!);
                    Emit3(Opcode.PUSH_CONST, ci);
                    argCount++;
                }
                else
                {
                    int line = GetLineNumber(path);
                    diagnostics.Report(pathToFileComp, $"'{fullName}' is missing required argument '{param.Name}' at line {line}");
                    return;
                }
            }

            foreach (var argName in namedArgs.Keys)
            {
                if (!sig.Params.Any(p => p.Name == argName))
                {
                    int line = GetLineNumber(path + ".args");
                    diagnostics.Report(pathToFileComp, $"'{fullName}' has no parameter named '{argName}' at line {line}");
                    return;
                }
            }

            if (expectsValue && sig.ReturnType == null)
            {
                int line = GetLineNumber(path);
                diagnostics.Report(pathToFileComp, $"'{fullName}' is void and cannot be used as an expression at line {line}");
                return;
            }

            int nameIndex = AddConstant(new Value(fullName));
            Emit3(Opcode.PUSH_CONST, nameIndex);
            bytecode.Add(Opcode.CALL);
            bytecode.Add((Opcode)(byte)argCount);
        }

        private void CompileExpression(JsonElement expr, string path)
        {
            switch (expr.ValueKind)
            {
                case JsonValueKind.Number:
                {
                    if (expr.TryGetInt32(out int intVal))
                    {
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value(intVal)));
                    }
                    else if (expr.TryGetDouble(out double dbl))
                    {
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value(dbl)));
                    }
                    else
                    {
                        diagnostics.Report(pathToFileComp, $"Unsupported numeric value at line {GetLineNumber(path)}");
                    }
                    break;
                }

                case JsonValueKind.String:
                {
                    string name = expr.GetString()!;
                    if (locals.TryGetValue(name, out var info))
                    {
                        if (!assignedLocals.Contains(name))
                        {
                            diagnostics.Report(pathToFileComp, $"Variable '{name}' is used before being assigned at line {GetLineNumber(path)}");
                            break;
                        }
                        Emit3(Opcode.LOAD_LOCAL, info.Index);
                    }
                    else
                    {
                        diagnostics.Report(pathToFileComp, $"Undefined variable '{name}' at line {GetLineNumber(path)}");
                    }
                    break;
                }

                case JsonValueKind.True:
                case JsonValueKind.False:
                    Emit3(Opcode.PUSH_CONST, AddConstant(new Value(expr.GetBoolean())));
                    break;

                case JsonValueKind.Array:
                {
                    int count = 0;
                    foreach (var element in expr.EnumerateArray())
                    {
                        CompileExpression(element, path + $"[{count++}]");
                    }
                    bytecode.Add(Opcode.MAKE_ARRAY);
                    bytecode.Add((Opcode)(byte)count);
                    break;
                }

                case JsonValueKind.Null:
                    Emit3(Opcode.PUSH_CONST, AddConstant(new Value(0, "void")));
                    break;

                case JsonValueKind.Object:
                {
                    if (expr.TryGetProperty("string", out var strProp))
                    {
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value(strProp.GetString()!)));
                        break;
                    }

                    if (expr.TryGetProperty("call", out var callExpr))
                    {
                        CompileCall(callExpr, path + ".call", expectsValue: true);
                        break;
                    }

                    if (expr.TryGetProperty("get", out var getExpr))
                    {
                        string arrName = getExpr.GetProperty("array").GetString()!;
                        if (!locals.TryGetValue(arrName, out var arrInfo))
                        {
                            diagnostics.Report(pathToFileComp, $"Undefined variable '{arrName}' at line {GetLineNumber(path + ".get.array")}");
                            break;
                        }
                        Emit3(Opcode.LOAD_LOCAL, arrInfo.Index);
                        CompileExpression(getExpr.GetProperty("at"), path + ".get.at");
                        bytecode.Add(Opcode.ARRAY_GET);
                        break;
                    }

                    if (expr.TryGetProperty("not", out var notExpr))
                    {
                        CompileExpression(notExpr, path + ".not");
                        bytecode.Add(Opcode.NOT);
                        break;
                    }

                    if (expr.TryGetProperty("length", out var lenExpr))
                    {
                        if (lenExpr.ValueKind == JsonValueKind.String)
                        {
                            // bare variable name
                            string varName = lenExpr.GetString()!;
                            if (!locals.TryGetValue(varName, out var varInfo))
                            {
                                diagnostics.Report(pathToFileComp, $"Undefined variable '{varName}' at line {GetLineNumber(path + ".length")}");
                                break;
                            }
                            if (varInfo.Type != JSType.ARRAY && varInfo.Type != JSType.STRING)
                            {
                                diagnostics.Report(pathToFileComp, $"'{varName}' is of type '{varInfo.Type}' and does not have a length at line {GetLineNumber(path + ".length")}");
                                break;
                            }
                            Emit3(Opcode.LOAD_LOCAL, varInfo.Index);
                        }
                        else
                        {
                            // expression that produces a string or array
                            CompileExpression(lenExpr, path + ".length");
                        }

                        bytecode.Add(Opcode.ARRAY_LEN);
                        break;
                    }

                    if (expr.TryGetProperty("name", out var nameProp))
                    {
                        string varName = nameProp.GetString()!;
                        if (!locals.TryGetValue(varName, out var varInfo))
                        {
                            diagnostics.Report(pathToFileComp, $"Undefined variable '{varName}' at line {GetLineNumber(path + ".name")}");
                            break;
                        }
                        Emit3(Opcode.LOAD_LOCAL, varInfo.Index);
                        break;
                    }

                    if (expr.TryGetProperty("and", out var andExpr))
                    {
                        CompileLogical(andExpr, Opcode.AND, path + ".and");
                        break;
                    }

                    if (expr.TryGetProperty("or", out var orExpr))
                    {
                        CompileLogical(orExpr, Opcode.OR, path + ".or");
                        break;
                    }

                    foreach (var prop in expr.EnumerateObject())
                    {
                        string opName = prop.Name;
                        var operands = prop.Value;

                        Opcode opCode;
                        switch (opName)
                        {
                            case "add":
                                opCode = Opcode.ADD;
                                break;
                            case "subtract":
                                opCode = Opcode.SUB;
                                break;
                            case "multiply":
                                opCode = Opcode.MUL;
                                break;
                            case "divide":
                                opCode = Opcode.DIV;
                                break;
                            case "eq":
                                opCode = Opcode.EQ;
                                break;
                            case "gt":
                                opCode = Opcode.GT;
                                break;
                            case "lt":
                                opCode = Opcode.LT;
                                break;
                            default:
                                diagnostics.Report(pathToFileComp, $"Unknown operation '{opName}' at line {GetLineNumber(path + "." + opName)}");
                                continue;
                        }

                        int operandCount = 0;
                        if (operands.ValueKind == JsonValueKind.Array)
                        {
                            int index = 0;
                            foreach (var operand in operands.EnumerateArray())
                            {
                                JSType? operandType = InferType(operand);
                                if (operandType.HasValue && operandType.Value == JSType.BOOL && opName != "eq" && opName != "gt" && opName != "lt")
                                {
                                    diagnostics.Report(pathToFileComp, $"Cannot use bool in '{opName}' expression at line {GetLineNumber(path + "." + opName)}");
                                    return;
                                }
                                CompileExpression(operand, path + $".{opName}[{index++}]");
                                operandCount++;
                            }
                        }
                        else
                        {
                            CompileExpression(operands, path + "." + opName);
                            operandCount = 1;
                        }

                        bytecode.Add(opCode);
                        bytecode.Add((Opcode)(byte)operandCount);
                    }
                    break;
                }

                default:
                    diagnostics.Report(pathToFileComp, $"Unsupported expression kind '{expr.ValueKind}' at line {GetLineNumber(path)}");
                    break;
            }
        }

        private void CompileIf(JsonElement ifExpr, string path)
        {
            if (!ifExpr.TryGetProperty("condition", out var condition))
            {
                diagnostics.Report(pathToFileComp, $"'if' is missing 'condition' at line {GetLineNumber(path)}");
                return;
            }

            CompileExpression(condition, path + ".condition");

            bytecode.Add(Opcode.JMP_IF_FALSE);
            int falseJumpIndex = bytecode.Count;
            bytecode.Add(0x00);
            bytecode.Add(0x00);

            if (!ifExpr.TryGetProperty("then", out var then))
            {
                diagnostics.Report(pathToFileComp, $"'if' is missing 'then' at line {GetLineNumber(path)}");
                return;
            }

            int stmtIndex = 0;
            foreach (var stmt in then.EnumerateArray())
            {
                CompileStatement(stmt, path + $".then[{stmtIndex++}]");
            }

            bool hasElse = ifExpr.TryGetProperty("else", out var elseBranch);
            int elseJumpIndex = -1;
            if (hasElse)
            {
                bytecode.Add(Opcode.JMP);
                elseJumpIndex = bytecode.Count;
                bytecode.Add(0x00);
                bytecode.Add(0x00);
            }

            int afterThen = bytecode.Count;
            bytecode[falseJumpIndex] = (Opcode)(byte)(afterThen & 0xFF);
            bytecode[falseJumpIndex + 1] = (Opcode)(byte)((afterThen >> 8) & 0xFF);

            if (hasElse)
            {
                stmtIndex = 0;
                foreach (var stmt in elseBranch.EnumerateArray())
                {
                    CompileStatement(stmt, path + $".else[{stmtIndex++}]");
                }

                int afterElse = bytecode.Count;
                bytecode[elseJumpIndex] = (Opcode)(byte)(afterElse & 0xFF);
                bytecode[elseJumpIndex + 1] = (Opcode)(byte)((afterElse >> 8) & 0xFF);
            }
        }

        private void CompileWhile(JsonElement whileExpr, string path)
        {
            if (!whileExpr.TryGetProperty("condition", out var condition))
            {
                diagnostics.Report(pathToFileComp, $"'while' is missing 'condition' at line {GetLineNumber(path)}");
                return;
            }

            if (!whileExpr.TryGetProperty("body", out var body))
            {
                diagnostics.Report(pathToFileComp, $"'while' is missing 'body' at line {GetLineNumber(path)}");
                return;
            }

            int loopStart = bytecode.Count;
            CompileExpression(condition, path + ".condition");

            bytecode.Add(Opcode.JMP_IF_FALSE);
            int exitJumpIndex = bytecode.Count;
            bytecode.Add(0x00);
            bytecode.Add(0x00);

            int stmtIndex = 0;
            foreach (var stmt in body.EnumerateArray())
            {
                CompileStatement(stmt, path + $".body[{stmtIndex++}]");
            }

            bytecode.Add(Opcode.JMP);
            bytecode.Add((Opcode)(byte)(loopStart & 0xFF));
            bytecode.Add((Opcode)(byte)(loopStart >> 8));

            int loopEnd = bytecode.Count;
            bytecode[exitJumpIndex] = (Opcode)(byte)(loopEnd & 0xFF);
            bytecode[exitJumpIndex + 1] = (Opcode)(byte)((loopEnd >> 8) & 0xFF);
        }

        private void CompileLogical(JsonElement operands, Opcode op, string path)
        {
            if (operands.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Report(pathToFileComp, $"'{op}' requires an array of conditions at line {GetLineNumber(path)}");
                return;
            }

            int count = 0;
            int index = 0;
            foreach (var operand in operands.EnumerateArray())
            {
                CompileExpression(operand, path + $"[{index++}]");
                count++;
            }

            bytecode.Add(op);
            bytecode.Add((Opcode)(byte)count);
        }

        public void RegisterFile(string path)
        {
            string jsonText;
            try
            {
                jsonText = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                diagnostics.Report(Path.GetFullPath(path), $"Could not read file: {e.Message}");
                return;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonText);
            }
            catch (Exception e)
            {
                diagnostics.Report(Path.GetFullPath(path), $"Malformed JSON: {e.Message}");
                return;
            }

            using (doc)
            {
                var root = doc.RootElement;
                string ns = root.TryGetProperty("namespace", out var nsProp) ? nsProp.GetString() ?? "Global" : "Global";

                if (!root.TryGetProperty("functions", out var functions))
                    return;

                foreach (var func in functions.EnumerateArray())
                {
                    string funcName = func.GetProperty("name").GetString()!;
                    string fullName = $"{ns}.{funcName}";

                    if (signatureTable.ContainsKey(fullName))
                    {
                        diagnostics.Report(Path.GetFullPath(path), $"Duplicate function '{fullName}' already defined in another file");
                        continue;
                    }

                    var paramList = new List<FunctionParam>();
                    if (func.TryGetProperty("params", out var paramsEl))
                    {
                        foreach (var param in paramsEl.EnumerateArray())
                        {
                            string paramName = param.GetProperty("name").GetString()!;
                            if (param.TryGetProperty("default", out var defaultVal))
                            {
                                Value defaultValue = defaultVal.ValueKind switch
                                {
                                    JsonValueKind.Number when defaultVal.TryGetInt64(out long l) => new Value(l),
                                    JsonValueKind.Number => new Value(defaultVal.GetDouble()),
                                    JsonValueKind.String => new Value(defaultVal.GetString()!),
                                    JsonValueKind.True => new Value(true),
                                    JsonValueKind.False => new Value(false),
                                    _ => new Value()
                                };
                                paramList.Add(new FunctionParam(paramName, true, defaultValue));
                            }
                            else
                            {
                                paramList.Add(new FunctionParam(paramName));
                            }
                        }
                    }

                    JSType? returnType = null;
                    if (func.TryGetProperty("type", out var retTypeProp))
                    {
                        string retTypeStr = retTypeProp.GetString() ?? "";
                        var (baseType, _, _) = ParseType(retTypeStr);
                        returnType = baseType;
                    }

                    signatureTable[fullName] = new FunctionSignature(fullName, paramList, returnType);
                }
            }
        }

        public void RegisterNativeNamespaces()
        {
            // Gfx.DrawRect(x, y, w, h, r, g, b)
            signatureTable["Gfx.DrawRect"] = new FunctionSignature("Gfx.DrawRect", new List<FunctionParam>
            {
                new FunctionParam("x"),
                new FunctionParam("y"),
                new FunctionParam("w"),
                new FunctionParam("h"),
                new FunctionParam("r"),
                new FunctionParam("g"),
                new FunctionParam("b"),
            }, null);

            signatureTable["Gfx.GetLayer"]  = new FunctionSignature("Gfx.GetLayer",  new List<FunctionParam>(), JSType.POINTER);
            signatureTable["Gfx.GetDevice"] = new FunctionSignature("Gfx.GetDevice", new List<FunctionParam>(), JSType.POINTER);
        }

        private void Emit3(Opcode op, int index)
        {
            bytecode.Add(op);
            bytecode.Add((Opcode)(byte)(index & 0xFF));
            bytecode.Add((Opcode)((index >> 8) & 0xFF));
        }

        private (JSType type, JSType? elementType, string? pointeeType) ParseType(string typeStr)
        {
            // Pointer type — recursive
            if (typeStr.StartsWith("*"))
            {
                string inner = typeStr.Substring(1);
                return (JSType.POINTER, null, inner);
            }

            return typeStr switch
            {
                "int" => (JSType.INT, null, null),
                "float" => (JSType.FLOAT, null, null),
                "string" => (JSType.STRING, null, null),
                "bool" => (JSType.BOOL, null, null),
                "int[]" => (JSType.ARRAY, JSType.INT, null),
                "float[]" => (JSType.ARRAY, JSType.FLOAT, null),
                "string[]" => (JSType.ARRAY, JSType.STRING, null),
                "bool[]" => (JSType.ARRAY, JSType.BOOL, null),
                _ => throw new Exception($"Unknown type '{typeStr}'")
            };
        }

        private JSType? InferType(JsonElement expr)
        {
            return expr.ValueKind switch
            {
                JsonValueKind.Number => expr.TryGetInt64(out _) ? JSType.INT : JSType.FLOAT,
                JsonValueKind.True => JSType.BOOL,
                JsonValueKind.False => JSType.BOOL,
                JsonValueKind.Array => JSType.ARRAY,
                JsonValueKind.Object => InferTypeFromObject(expr),
                JsonValueKind.String => locals.TryGetValue(expr.GetString()!, out var info) ? info.Type : null,
                _ => null
            };
        }

        private JSType? InferTypeFromObject(JsonElement expr)
        {
            if (expr.TryGetProperty("string", out _))
                return JSType.STRING;

            if (expr.TryGetProperty("cast", out _))
                return JSType.POINTER;

            if (expr.TryGetProperty("get", out _))
                return null;

            if (expr.TryGetProperty("not", out _))
                return JSType.BOOL;

            if (expr.TryGetProperty("length", out _))
                return JSType.INT;

            if (expr.TryGetProperty("add", out _) || expr.TryGetProperty("subtract", out _) || expr.TryGetProperty("multiply", out _) || expr.TryGetProperty("divide", out _))
                return JSType.FLOAT;

            if (expr.TryGetProperty("eq", out _) || expr.TryGetProperty("gt", out _) || expr.TryGetProperty("lt", out _) || expr.TryGetProperty("and", out _) || expr.TryGetProperty("or", out _))
                return JSType.BOOL;

            if (expr.TryGetProperty("call", out var callEl))
            {
                if (callEl.TryGetProperty("namespace", out var ns) && callEl.TryGetProperty("function", out var fn))
                {
                    string fullName = $"{ns.GetString()}.{fn.GetString()}";
                    if (signatureTable.TryGetValue(fullName, out var sig) && sig.ReturnType.HasValue)
                        return sig.ReturnType.Value;
                }
                return null;
            }

            // String variable reference resolves to its declared type
            if (expr.ValueKind == JsonValueKind.String)
            {
                string varName = expr.GetString()!;
                if (locals.TryGetValue(varName, out var info))
                    return info.Type;
            }

            return null;
        }

        private static bool TypesCompatible(JSType declared, JSType assigned, string? declaredPointee = null, string? assignedPointee = null)
        {
            if (declared == assigned)
            {
                // Both pointers — check pointee types
                if (declared == JSType.POINTER)
                {
                    if (declaredPointee == "void" || assignedPointee == "void")
                        return true;

                    return declaredPointee == assignedPointee;
                }
                return true;
            }

            // int/float interop
            if ((declared == JSType.INT || declared == JSType.FLOAT) && (assigned == JSType.INT || assigned == JSType.FLOAT))
                return true;

            return false;
        }

        private int AddConstant(Value val)
        {
            constants.Add(val);
            return constants.Count - 1;
        }
    }
}