using JSONScript.Runtime;

namespace JSONScript.Compiler
{
    public class FunctionParam
    {
        public string Name { get; }
        public bool HasDefault { get; }
        public Value? Default { get; }

        public FunctionParam(string name, bool hasDefault = false, Value? defaultValue = null)
        {
            Name = name;
            HasDefault = hasDefault;
            Default = defaultValue;
        }
    }

    public class FunctionSignature
    {
        public string Name;
        public List<FunctionParam> Params;
        public JSType? ReturnType; // null means void

        public FunctionSignature(string name, List<FunctionParam> param, JSType? returnType = null)
        {
            Name = name;
            Params = param;
            ReturnType = returnType;
        }
    }
}