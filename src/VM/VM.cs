using System;
using System.Collections.Generic;
using JSONScript.Runtime;
using JSONScript.Compiler;

namespace JSONScript.VM
{
    public class VM
    {
        private readonly Dictionary<string, CompiledFunction> functionTable;
        private readonly List<Value> stack = new();
        private readonly Stack<CallFrame> callStack = new();

        public VM(Dictionary<string, CompiledFunction> functionTable, string entryPoint)
        {
            this.functionTable = functionTable;

            if (!functionTable.TryGetValue(entryPoint, out var main))
                throw new Exception($"Entry point '{entryPoint}' not found");

            callStack.Push(new CallFrame
            {
                Bytecode = main.Bytecode,
                Constants = main.Constants,
                InstructionPointer = 0,
                Locals = new Value[main.LocalCount]
            });
        }

        public void Run()
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

                        // Name is on top, pop it first
                        string funcName = Pop().StringValue!;

                        // Then pop args in reverse
                        var args = new Value[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            args[i] = Pop();
                        }

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

                    case Opcode.JMP_IF_FALSE:
                    {
                        int target = ReadUInt16(frame);
                        var val = Pop();
                        bool isFalse = val.Type == JSType.BOOL && !val.BoolValue;

                        if (isFalse)
                            frame.InstructionPointer = target;

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
                            throw new Exception($"Array index {index} out of bounds (length {arrVal.ArrayValue!.Count})");

                        stack.Add(arrVal.ArrayValue[index]);
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
                            throw new Exception($"Array index {index} out of bounds (length {arrVal.ArrayValue!.Count})");

                        arrVal.ArrayValue[index] = val;
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
                        Value arrVal = Pop();
                        
                        if (arrVal.Type != JSType.ARRAY)
                            throw new Exception("ARRAY_LEN called on non-array value");

                        stack.Add(new Value(arrVal.ArrayValue!.Count));
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