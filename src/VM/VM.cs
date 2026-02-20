using System;
using System.Collections.Generic;
using JSONScript.Runtime;
using JSONScript.Compiler;
using JSONScript.VM.Graphics;
using JSONScript.VM.Graphics.Metal;
using System.Runtime.InteropServices;

namespace JSONScript.VM
{
    public class VM
    {
        private readonly Dictionary<string, CompiledFunction> functionTable;

        private readonly Dictionary<string, List<string>> eventHandlers = new();
        private readonly List<Value> stack = new();
        private readonly Stack<CallFrame> callStack = new();

        public AppKitWindow? graphicsWindow;

        private IGraphicsBackend? backend;

        public void SetGraphicsBackend(IGraphicsBackend b) => backend = b;

        public IGraphicsBackend? GetGraphicsBackend() => backend;

        public VM(Dictionary<string, CompiledFunction> functionTable, string entryPoint, string[] scriptArgs)
        {
            this.functionTable = functionTable;

            if (!functionTable.TryGetValue(entryPoint, out var main))
                throw new Exception($"Entry point '{entryPoint}' not found");

            var frame = new CallFrame
            {
                Bytecode = main.Bytecode,
                Constants = main.Constants,
                InstructionPointer = 0,
                Locals = new Value[main.LocalCount]
            };

            // Inject script args as a string[] into the first param slot
            var argValues = scriptArgs.Select(a => new Value(a)).ToList();
            frame.Locals[0] = new Value(argValues, JSType.STRING);

            callStack.Push(frame);
        }

        public void Run()
        {
            while (callStack.Count > 0)
                RunStep();
        }

        public void FireEvent(string eventName, Value[] args)
        {
            if (!eventHandlers.TryGetValue(eventName, out var handlers))
                return;
            foreach (var funcName in handlers)
                CallFunction(funcName, args);
        }

        public void RunStep()
        {
            while (callStack.Count > 0)
            {
                var frame = callStack.Peek();

                if (frame.InstructionPointer >= frame.Bytecode.Length)
                    break;

                Opcode opcode = (Opcode)frame.Bytecode[frame.InstructionPointer++];

                if (Program.Debug)
                    Console.WriteLine($"Opcode: {opcode}, IP: {frame.InstructionPointer}, Stack: [{string.Join(", ", stack)}]");

                switch (opcode)
                {
                    case Opcode.PUSH_CONST:
                    {
                        int index = ReadUInt16(frame);
                        stack.Add(frame.Constants[index]);
                        break;
                    }

                    case Opcode.LOAD_LOCAL:
                    {
                        int index = ReadUInt16(frame);
                        stack.Add(frame.Locals[index]);
                        break;
                    }

                    case Opcode.STORE_LOCAL:
                    {
                        int index = ReadUInt16(frame);
                        frame.Locals[index] = Pop();
                        break;
                    }

                    case Opcode.ADD:
                        ExecuteBinary(frame, AddValues);
                        break;
                    case Opcode.SUB:
                        ExecuteBinary(frame, SubtractValues);
                        break;
                    case Opcode.MUL:
                        ExecuteBinary(frame, MultiplyValues);
                        break;
                    case Opcode.DIV:
                        ExecuteBinary(frame, DivideValues);
                        break;

                    case Opcode.CALL:
                    {
                        int argCount = frame.Bytecode[frame.InstructionPointer++];
                        string funcName = Pop().StringValue!;

                        var args = new Value[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                            args[i] = Pop();

                        // Native namespace intercept
                        if (funcName.StartsWith("Gfx."))
                        {
                            ExecuteNative(funcName, args);
                            break;
                        }

                        // normal function lookup
                        if (!functionTable.TryGetValue(funcName, out var target))
                            throw new Exception($"Unknown function '{funcName}'");

                        var newFrame = new CallFrame
                        {
                            Bytecode = target.Bytecode,
                            Constants = target.Constants,
                            InstructionPointer = 0,
                            Locals = new Value[target.LocalCount]
                        };

                        for (int i = 0; i < argCount; i++)
                            newFrame.Locals[i] = args[i];

                        callStack.Push(newFrame);
                        break;
                    }

                    case Opcode.RET:
                    {
                        Value returnValue = stack.Count > 0 ? Pop() : new Value();
                        callStack.Pop();
                        // Push return value onto caller's stack

                        if (callStack.Count > 0)
                            stack.Add(returnValue);

                        break;
                    }

                    case Opcode.PRINT:
                    {
                        Console.WriteLine(Pop());
                        break;
                    }

                    case Opcode.EQ:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var values = new List<Value>();

                        for (int i = 0; i < count; i++)
                        {
                            values.Add(Pop());
                        }

                        values.Reverse();
                        // Chain equality: a == b == c
                        bool result = true;

                        for (int i = 0; i < values.Count - 1; i++)
                        {
                            result &= ValuesEqual(values[i], values[i + 1]);
                        }

                        stack.Add(new Value(result));

                        break;
                    }

                    case Opcode.GT:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var values = new List<Value>();

                        for (int i = 0; i < count; i++)
                        {
                            values.Add(Pop());
                        }

                        values.Reverse();
                        bool result = true;

                        for (int i = 0; i < values.Count - 1; i++)
                        {
                            result &= values[i].AsDouble > values[i + 1].AsDouble;
                        }

                        stack.Add(new Value(result));

                        break;
                    }

                    case Opcode.LT:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var values = new List<Value>();
                        for (int i = 0; i < count; i++)
                        {
                            values.Add(Pop());
                        }

                        values.Reverse();
                        bool result = true;

                        for (int i = 0; i < values.Count - 1; i++)
                        {
                            result &= values[i].AsDouble < values[i + 1].AsDouble;
                        }
                            
                        stack.Add(new Value(result));

                        break;
                    }

                    case Opcode.JMP:
                    {
                        int target = ReadUInt16(frame);
                        frame.InstructionPointer = target;

                        break;
                    }

                    case Opcode.NOT:
                    {
                        Value val = Pop();
                        if (val.Type != JSType.BOOL)
                            throw new Exception($"Cannot apply 'not' to non-bool value of type '{val.Type}'");
                        stack.Add(new Value(!val.BoolValue));
                        break;
                    }

                    case Opcode.JMP_IF_FALSE:
                    {
                        int target = ReadUInt16(frame);
                        var val = Pop();
                        bool isFalse = val.Type switch
                        {
                            JSType.BOOL    => !val.BoolValue,
                            JSType.POINTER => val.PointerValue == 0,
                            JSType.INT     => val.AsInt == 0,
                            _              => false
                        };

                        if (isFalse)
                            frame.InstructionPointer = target;

                        break;
                    }

                    case Opcode.FFI_CALL:
                    {
                        int argCount  = frame.Bytecode[frame.InstructionPointer++];
                        string symbol = Pop().StringValue!;
                        string lib    = Pop().StringValue!;
                        string retType = Pop().StringValue!;

                        var args = new (string type, Value val)[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            var val     = Pop();
                            var argType = Pop().StringValue!;
                            args[i] = (argType, val);
                        }

                        var result = FFI.FFIInvoker.Invoke(lib, symbol, args, retType);

                        if (retType != "void")
                            stack.Add(result);

                        break;
                    }

                    case Opcode.AND:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var values = new List<Value>();

                        for (int i = 0; i < count; i++)
                        {
                            values.Add(Pop());
                        }

                        values.Reverse();
                        bool result = values.All(v => v.Type == JSType.BOOL && v.BoolValue);
                        stack.Add(new Value(result));

                        break;
                    }

                    case Opcode.OR:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var values = new List<Value>();

                        for (int i = 0; i < count; i++)
                        {
                            values.Add(Pop());
                        }

                        values.Reverse();
                        bool result = values.Any(v => v.Type == JSType.BOOL && v.BoolValue);
                        stack.Add(new Value(result));

                        break;
                    }

                    case Opcode.STORE_LOCAL_INT:
                    {
                        int index = ReadUInt16(frame);
                        Value val = Pop();
                        // Truncate to int regardless of value type
                        frame.Locals[index] = val.Type == JSType.FLOAT ? new Value((long)val.FloatValue) : new Value(val.AsInt);
                        break;
                    }

                    case Opcode.MAKE_ARRAY:
                    {
                        int count = frame.Bytecode[frame.InstructionPointer++];
                        var elements = new List<Value>(count);

                        for (int i = 0; i < count; i++)
                            elements.Add(new Value()); // placeholder

                        for (int i = count - 1; i >= 0; i--)
                            elements[i] = Pop();

                        // Infer element type from first element
                        JSType elementType = elements.Count > 0 ? elements[0].Type : JSType.NULL;

                        // Runtime type check — all elements must match
                        foreach (var el in elements)
                        {
                            if (el.Type != elementType && !(el.Type == JSType.INT && elementType == JSType.FLOAT) && !(el.Type == JSType.FLOAT && elementType == JSType.INT))
                                throw new Exception($"Array element type mismatch: expected '{elementType}' but got '{el.Type}'");
                        }

                        stack.Add(new Value(elements, elementType));
                        break;
                    }

                    case Opcode.ARRAY_GET:
                    {
                        Value indexVal = Pop();
                        Value arrVal = Pop();

                        if (arrVal.Type != JSType.ARRAY)
                            throw new Exception("ARRAY_GET called on non-array value");

                        int index = (int)indexVal.AsInt;
                        if (index < 0 || index >= arrVal.ArrayValue!.Count)
                        {
                            Console.Error.WriteLine($"Runtime error: Array index {index} out of bounds (length {arrVal.ArrayValue!.Count})");
                            Environment.Exit(1);
                        }
                        stack.Add(arrVal.ArrayValue![index]);
                        break;
                    }

                    case Opcode.ARRAY_SET:
                    {
                        Value val = Pop();
                        Value indexVal = Pop();
                        Value arrVal = Pop();

                        if (arrVal.Type != JSType.ARRAY)
                            throw new Exception("ARRAY_SET called on non-array value");

                        int index = (int)indexVal.AsInt;
                        if (index < 0 || index >= arrVal.ArrayValue!.Count)
                        {
                            Console.Error.WriteLine($"Runtime error: Array index {index} out of bounds (length {arrVal.ArrayValue!.Count})");
                            Environment.Exit(1);
                        }

                        arrVal.ArrayValue[index] = val;
                        break;
                    }

                    case Opcode.ON_EVENT:
                    {
                        string funcName  = Pop().StringValue!;  // funcIndex was pushed last, so on top
                        string eventName = Pop().StringValue!;  // eventIndex was pushed first, so below
                        if (!eventHandlers.ContainsKey(eventName))
                            eventHandlers[eventName] = new List<string>();
                        eventHandlers[eventName].Add(funcName);
                        break;
                    }

                    case Opcode.ARRAY_PUSH:
                    {
                        Value val = Pop();
                        Value arrVal = Pop();
                        if (arrVal.Type != JSType.ARRAY)
                            throw new Exception("ARRAY_PUSH called on non-array value");
                        // Runtime type check
                        if (arrVal.ElementType.HasValue && val.Type != arrVal.ElementType.Value)
                            throw new Exception($"Cannot push '{val.Type}' into '{arrVal.ElementType}[]' array");

                        arrVal.ArrayValue!.Add(val);
                        break;
                    }

                    case Opcode.ARRAY_LEN:
                    {
                        Value val = Pop();
                        if (val.Type == JSType.ARRAY)
                        {
                            stack.Add(new Value(val.ArrayValue!.Count));
                        }
                        else if (val.Type == JSType.STRING)
                        {
                            stack.Add(new Value(val.StringValue!.Length));
                        }
                        else
                        {
                            throw new Exception($"Cannot get length of type '{val.Type}'");
                        }
                        break;
                    }

                    case Opcode.MEM_ALLOC:
                    {
                        int size = (int)Pop().AsInt;
                        nint ptr = Marshal.AllocHGlobal(size);
                        // zero out the memory
                        for (int i = 0; i < size; i++)
                            Marshal.WriteByte(ptr + i, 0);
                        stack.Add(new Value(ptr, "void"));
                        break;
                    }

                    case Opcode.MEM_WRITE:
                    {
                        string type   = Pop().StringValue!;
                        Value  val    = Pop();
                        int    offset = (int)Pop().AsInt;
                        nint   ptr    = Pop().PointerValue;

                        switch (type)
                        {
                            case "int8":   Marshal.WriteByte(ptr + offset,  (byte)val.AsInt);                           break;
                            case "int16":  Marshal.WriteInt16(ptr + offset, (short)val.AsInt);                          break;
                            case "int32":  Marshal.WriteInt32(ptr + offset, (int)val.AsInt);                            break;
                            case "int64":  Marshal.WriteInt64(ptr + offset, val.AsInt);                                 break;
                            case "float32":Marshal.WriteInt32(ptr + offset, BitConverter.SingleToInt32Bits((float)val.AsDouble)); break;
                            case "float64":Marshal.WriteInt64(ptr + offset, BitConverter.DoubleToInt64Bits(val.AsDouble));        break;
                            case "ptr":    Marshal.WriteIntPtr(ptr + offset, val.PointerValue);                         break;
                            default: throw new Exception($"memwrite: unknown type '{type}'");
                        }
                        break;
                    }

                    case Opcode.MEM_READ:
                    {
                        string type   = Pop().StringValue!;
                        int    offset = (int)Pop().AsInt;
                        nint   ptr    = Pop().PointerValue;

                        Value result = type switch
                        {
                            "int8"    => new Value((long)Marshal.ReadByte(ptr + offset)),
                            "int16"   => new Value((long)Marshal.ReadInt16(ptr + offset)),
                            "int32"   => new Value((long)Marshal.ReadInt32(ptr + offset)),
                            "int64"   => new Value(Marshal.ReadInt64(ptr + offset)),
                            "float32" => new Value((double)BitConverter.Int32BitsToSingle(Marshal.ReadInt32(ptr + offset))),
                            "float64" => new Value(BitConverter.Int64BitsToDouble(Marshal.ReadInt64(ptr + offset))),
                            "ptr"     => new Value(Marshal.ReadIntPtr(ptr + offset), "void"),
                            _ => throw new Exception($"memread: unknown type '{type}'")
                        };
                        stack.Add(result);
                        break;
                    }

                    case Opcode.MEM_FREE:
                    {
                        nint ptr = Pop().PointerValue;
                        Marshal.FreeHGlobal(ptr);
                        break;
                    }

                    case Opcode.HALT:
                        callStack.Pop();

                        break;

                    default:
                        throw new Exception($"Unknown opcode {opcode}");
                }
            }
        }

        private static int ReadUInt16(CallFrame frame)
        {
            int low = frame.Bytecode[frame.InstructionPointer++];
            int high = frame.Bytecode[frame.InstructionPointer++];
            return low | (high << 8);
        }

        private Value Pop()
        {
            if (stack.Count == 0)
                throw new Exception("Stack underflow");

            var val = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return val;
        }

        private void ExecuteBinary(CallFrame frame, Func<Value, Value, Value> op)
        {
            int count = frame.Bytecode[frame.InstructionPointer++];
            var values = new List<Value>();

            for (int i = 0; i < count; i++)
            {
                values.Add(Pop()); 
            }

            values.Reverse();
            Value result = values[0];

            for (int i = 1; i < values.Count; i++)
            {
                result = op(result, values[i]);
            }

            stack.Add(result);
        }

        private Value AddValues(Value a, Value b)
        {
            if (a.Type == JSType.STRING || b.Type == JSType.STRING)
                return new Value(a.ToString() + b.ToString());

            if (a.Type == JSType.FLOAT || b.Type == JSType.FLOAT)
                return new Value(a.AsDouble + b.AsDouble);

            if (a.Type == JSType.STRING && b.Type == JSType.INT)
                return new Value(a.ToString() + b.AsInt);

            return new Value(a.AsInt + b.AsInt);
        }

        private Value SubtractValues(Value a, Value b)
        {
            if (a.Type == JSType.FLOAT || b.Type == JSType.FLOAT)
                return new Value(a.AsDouble - b.AsDouble);

            return new Value(a.AsInt - b.AsInt);
        }

        private Value MultiplyValues(Value a, Value b)
        {
            if (a.Type == JSType.FLOAT || b.Type == JSType.FLOAT)
                return new Value(a.AsDouble * b.AsDouble);

            return new Value(a.AsInt * b.AsInt);
        }

        private void ExecuteNative(string funcName, Value[] args)
        {
            switch (funcName)
            {
                case "Gfx.DrawRect":
                {
                    if (backend == null)
                        throw new Exception("Gfx.DrawRect called but graphics mode is not enabled");

                    float x = (float)args[0].AsDouble;
                    float y = (float)args[1].AsDouble;
                    float w = (float)args[2].AsDouble;
                    float h = (float)args[3].AsDouble;
                    float r = (float)args[4].AsDouble;
                    float g = (float)args[5].AsDouble;
                    float b = (float)args[6].AsDouble;

                    backend.DrawRect(x, y, w, h, r, g, b);
                    break;
                }

                case "Gfx.GetLayer":
                    stack.Add(new Value(backend!.GetLayerPtr(), "void"));
                    break;
                case "Gfx.GetDevice":
                    stack.Add(new Value(backend!.GetDevicePtr(), "void"));
                    break;

                default:
                    throw new Exception($"Unknown native function '{funcName}'");
            }
        }

        private void CallFunction(string funcName, Value[] args)
        {
            if (!functionTable.TryGetValue(funcName, out var target))
            {
                return;
            }

            var newFrame = new CallFrame
            {
                Bytecode = target.Bytecode,
                Constants = target.Constants,
                InstructionPointer = 0,
                Locals = new Value[target.LocalCount]
            };

            for (int i = 0; i < args.Length && i < target.ParamCount; i++)
                newFrame.Locals[i] = args[i];

            callStack.Push(newFrame);

            // Run until this frame is done
            while (callStack.Count > 0 && callStack.Peek() == newFrame)
            {
                // reuse existing run logic
                RunStep();
            }
        }

        private bool ValuesEqual(Value a, Value b)
        {
            if (a.Type != b.Type)
            {
                // Allow int/float comparison
                if ((a.Type == JSType.INT || a.Type == JSType.FLOAT) && (b.Type == JSType.INT || b.Type == JSType.FLOAT))
                    return a.AsDouble == b.AsDouble;

                return false;
            }
            return a.Type switch
            {
                JSType.INT => a.IntValue == b.IntValue,
                JSType.FLOAT => a.FloatValue == b.FloatValue,
                JSType.STRING => a.StringValue == b.StringValue,
                JSType.BOOL => a.BoolValue == b.BoolValue,
                JSType.NULL => true,
                _ => false
            };
        }

        private Value DivideValues(Value a, Value b) => new Value(a.AsDouble / b.AsDouble);
    }
}