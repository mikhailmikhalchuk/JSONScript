using JSONScript.Runtime;

namespace JSONScript.VM
{
    public class CallFrame
    {
        public byte[] Bytecode = [];
        public Value[] Constants = [];
        public int InstructionPointer;
        public Value[] Locals = [];
    }
}