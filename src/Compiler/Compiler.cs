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
        private string currentNamespace = "";
        private string pathToFileComp = "";
        private Dictionary<string, int> elementLineNumbers = new();
        private readonly Dictionary<string, CompiledFunction> functionTable = new();
        private DiagnosticBag diagnostics = new();

        public Dictionary<string, CompiledFunction> FunctionTable => functionTable;

        private int GetLineNumber(string path) => elementLineNumbers.TryGetValue(path, out int line) ? line : 0;

        private struct LocalInfo
        {
            public int Index;
            public JSType Type;
            public JSType? ElementType;

            public LocalInfo(int index, JSType type, JSType? elementType = null)
            {
                Index = index;
                Type = type;
                ElementType = elementType;
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

            if (functionTable.ContainsKey(fullName))
            {
                diagnostics.Report(pathToFileComp, $"Duplicate function '{fullName}'");
                return;
            }

            int paramCount = 0;
            if (func.TryGetProperty("params", out var paramsEl))
            {
                foreach (var param in paramsEl.EnumerateArray())
                {
                    string paramName = param.GetProperty("name").GetString()!;
                    string typeStr = param.TryGetProperty("type", out var t) ? t.GetString() ?? "int" : "int";
                    var (baseType, elementType) = ParseType(typeStr);
                    locals[paramName] = new LocalInfo(locals.Count, baseType, elementType);
                    assignedLocals.Add(paramName);
                    paramCount++;
                }
            }

            if (func.TryGetProperty("locals", out var localsEl))
            {
                foreach (var loc in localsEl.EnumerateArray())
                {
                    string name = loc.GetProperty("name").GetString()!;
                    string typeStr = loc.GetProperty("type").GetString() ?? "int";
                    var (baseType, elementType) = ParseType(typeStr);
                    if (!locals.ContainsKey(name))
                        locals[name] = new LocalInfo(locals.Count, baseType, elementType);
                }
            }

            if (!func.TryGetProperty("body", out var body))
            {
                diagnostics.Report(pathToFileComp, $"Function '{fullName}' is missing a body");
                return;
            }

            int stmtIndex = 0;
            foreach (var stmt in body.EnumerateArray())
            {
                string stmtPath = $"{path}.body[{stmtIndex++}]";
                CompileStatement(stmt, stmtPath);
            }

            bytecode.Add(Opcode.HALT);

            functionTable[fullName] = new CompiledFunction(
                fullName, bytecode.Select(o => (byte)o).ToArray(), constants.ToArray(),
                locals.Count, paramCount);
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

                // Type check for non-array types
                if (localInfo.Type != JSType.ARRAY)
                {
                    JSType? inferredType = InferType(valueExpr);
                    if (inferredType.HasValue && !TypesCompatible(localInfo.Type, inferredType.Value))
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
                    bytecode.Add(Opcode.STORE_LOCAL_INT);
                else
                    bytecode.Add(Opcode.STORE_LOCAL);

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
            else if (stmt.TryGetProperty("return", out var returnExpr))
            {
                foreach (var prop in stmt.EnumerateObject())
                {
                    if (prop.Name != "return")
                    {
                        int line = GetLineNumber(path + "." + prop.Name);
                        diagnostics.Report(pathToFileComp, $"Unknown field '{prop.Name}' in return statement at line {line}");
                        return;
                    }
                }

                CompileExpression(returnExpr, path + ".return");
                bytecode.Add(Opcode.RET);
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

                CompileCall(callStmt, path + ".call");
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
            else
            {
                string firstKey = stmt.EnumerateObject().FirstOrDefault().Name ?? "(no keys)";
                int line = GetLineNumber(path + "." + firstKey);
                diagnostics.Report(pathToFileComp, $"Unsupported statement kind '{firstKey}' at line {line}");
            }
        }

        private void CompileCall(JsonElement callExpr, string path)
        {
            if (!callExpr.TryGetProperty("namespace", out var nsProp) ||
                !callExpr.TryGetProperty("function", out var funcProp))
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
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value((long)intVal)));
                    else if (expr.TryGetDouble(out double dbl))
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value(dbl)));
                    else
                        diagnostics.Report(pathToFileComp, $"Unsupported numeric value at line {GetLineNumber(path)}");
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

                case JsonValueKind.Object:
                {
                    if (expr.TryGetProperty("string", out var strProp))
                    {
                        Emit3(Opcode.PUSH_CONST, AddConstant(new Value(strProp.GetString()!)));
                        break;
                    }

                    if (expr.TryGetProperty("call", out var callExpr))
                    {
                        CompileCall(callExpr, path + ".call");
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

                    if (expr.TryGetProperty("length", out var lenExpr))
                    {
                        string arrName = lenExpr.GetString()!;
                        if (!locals.TryGetValue(arrName, out var arrInfo))
                        {
                            diagnostics.Report(pathToFileComp, $"Undefined variable '{arrName}' at line {GetLineNumber(path + ".length")}");
                            break;
                        }
                        Emit3(Opcode.LOAD_LOCAL, arrInfo.Index);
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
                            case "add":      opCode = Opcode.ADD; break;
                            case "subtract": opCode = Opcode.SUB; break;
                            case "multiply": opCode = Opcode.MUL; break;
                            case "divide":   opCode = Opcode.DIV; break;
                            case "eq":       opCode = Opcode.EQ;  break;
                            case "gt":       opCode = Opcode.GT;  break;
                            case "lt":       opCode = Opcode.LT;  break;
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
                CompileStatement(stmt, path + $".then[{stmtIndex++}]");

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
            bytecode[falseJumpIndex]     = (Opcode)(byte)(afterThen & 0xFF);
            bytecode[falseJumpIndex + 1] = (Opcode)(byte)((afterThen >> 8) & 0xFF);

            if (hasElse)
            {
                stmtIndex = 0;
                foreach (var stmt in elseBranch.EnumerateArray())
                    CompileStatement(stmt, path + $".else[{stmtIndex++}]");

                int afterElse = bytecode.Count;
                bytecode[elseJumpIndex]     = (Opcode)(byte)(afterElse & 0xFF);
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
                CompileStatement(stmt, path + $".body[{stmtIndex++}]");

            bytecode.Add(Opcode.JMP);
            bytecode.Add((Opcode)(byte)(loopStart & 0xFF));
            bytecode.Add((Opcode)(byte)(loopStart >> 8));

            int loopEnd = bytecode.Count;
            bytecode[exitJumpIndex]     = (Opcode)(byte)(loopEnd & 0xFF);
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
                        diagnostics.Report(Path.GetFullPath(path), $"Duplicate function '{fullName}' - already defined in another file");
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
                                    JsonValueKind.True   => new Value(true),
                                    JsonValueKind.False  => new Value(false),
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

                    signatureTable[fullName] = new FunctionSignature(fullName, paramList);
                }
            }
        }

        private void Emit3(Opcode op, int index)
        {
            bytecode.Add(op);
            bytecode.Add((Opcode)(byte)(index & 0xFF));
            bytecode.Add((Opcode)((index >> 8) & 0xFF));
        }

        private static (JSType baseType, JSType? elementType) ParseType(string type)
        {
            return type switch
            {
                "int" => (JSType.INT, null),
                "float" => (JSType.FLOAT, null),
                "string" => (JSType.STRING, null),
                "bool" => (JSType.BOOL, null),
                "int[]" => (JSType.ARRAY, JSType.INT),
                "float[]" => (JSType.ARRAY, JSType.FLOAT),
                "string[]" => (JSType.ARRAY, JSType.STRING),
                "bool[]" => (JSType.ARRAY, JSType.BOOL),
                _ => throw new Exception($"Unknown type '{type}'")
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

        private static JSType? InferTypeFromObject(JsonElement expr)
        {
            if (expr.TryGetProperty("string", out _))
                return JSType.STRING;

            if (expr.TryGetProperty("get", out _))
                return null; // depends on array element type

            if (expr.TryGetProperty("length", out _))
                return JSType.INT;

            if (expr.TryGetProperty("add", out _) || expr.TryGetProperty("subtract", out _) || expr.TryGetProperty("multiply", out _) || expr.TryGetProperty("divide", out _))
                return JSType.FLOAT;
            
            if (expr.TryGetProperty("eq", out _) || expr.TryGetProperty("gt", out _) || expr.TryGetProperty("lt", out _) || expr.TryGetProperty("and", out _) || expr.TryGetProperty("or", out _))
                return JSType.BOOL;

            if (expr.TryGetProperty("call", out _))
                return null;

            return null;
        }

        private static bool TypesCompatible(JSType declared, JSType assigned)
        {
            if (declared == assigned)
                return true;

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