namespace JSONScript.Compiler
{
    public class CompiledFunction
    {
        public string FullName { get; }   // e.g. "Math.Add"
        public byte[] Bytecode { get; }
        public Runtime.Value[] Constants { get; }
        public int LocalCount { get; }
        public int ParamCount { get; }

        public CompiledFunction(string fullName, byte[] bytecode, Runtime.Value[] constants, int localCount, int paramCount)
        {
            FullName = fullName;
            Bytecode = bytecode;
            Constants = constants;
            LocalCount = localCount;
            ParamCount = paramCount;
        }
    }
}