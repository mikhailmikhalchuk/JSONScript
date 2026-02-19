namespace JSONScript.Runtime
{
    public enum JSType
    {
        INT,
        FLOAT,
        STRING,
        BOOL,
        ARRAY,
        NULL
    }

    public class Value
    {
        public JSType Type;
        public long IntValue;
        public double FloatValue;
        public string? StringValue;
        public bool BoolValue;
        public List<Value>? ArrayValue;
        public JSType? ElementType; // the type of elements in the array

        public Value(long i)   { Type = JSType.INT;    IntValue = i; }
        public Value(double f) { Type = JSType.FLOAT;  FloatValue = f; }
        public Value(string s) { Type = JSType.STRING; StringValue = s; }
        public Value(bool b)   { Type = JSType.BOOL;   BoolValue = b; }
        public Value()         { Type = JSType.NULL; }

        public Value(List<Value> arr, JSType elementType)
        {
            Type = JSType.ARRAY;
            ArrayValue = arr;
            ElementType = elementType;
        }

        public long AsInt
        {
            get
            {
                return Type switch
                {
                    JSType.INT   => IntValue,
                    JSType.FLOAT => (long)FloatValue,
                    _ => throw new InvalidOperationException($"Cannot convert {Type} to int")
                };
            }
        }

        public double AsDouble
        {
            get
            {
                return Type switch
                {
                    JSType.INT   => IntValue,
                    JSType.FLOAT => FloatValue,
                    _ => throw new InvalidOperationException($"Cannot convert {Type} to double")
                };
            }
        }

        public override string ToString()
        {
            return Type switch
            {
                JSType.INT    => IntValue.ToString(),
                JSType.FLOAT  => FloatValue.ToString(),
                JSType.STRING => StringValue ?? "",
                JSType.BOOL   => BoolValue.ToString(),
                JSType.ARRAY  => $"[{string.Join(", ", ArrayValue!.Select(v => v.ToString()))}]",
                JSType.NULL   => "null",
                _             => "null"
            };
        }
    }
}