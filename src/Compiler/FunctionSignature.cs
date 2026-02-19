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
        public string FullName { get; }
        public List<FunctionParam> Params { get; }
        public int ParamCount => Params.Count;
        public int RequiredCount => Params.Count(p => !p.HasDefault);

        public FunctionSignature(string fullName, List<FunctionParam> parameters)
        {
            FullName = fullName;
            Params = parameters;
        }
    }
}