namespace JSONScript.VM
{
    public enum Opcode : byte
    {
        NOP = 0x00,
        PUSH_CONST = 0x01,
        LOAD_LOCAL = 0x02,
        STORE_LOCAL = 0x03,
        ADD = 0x04,
        SUB = 0x05,
        MUL = 0x06,
        DIV = 0x07,
        MOD = 0x08,
        NEG = 0x09,
        EQ = 0x0A,
        GT = 0x0B,
        LT = 0x0C,
        JMP = 0x0D,
        JMP_IF_FALSE = 0x0E,
        CALL = 0x0F,
        RET = 0x10,
        PRINT = 0x11,
        AND = 0x12,
        OR = 0x13,
        STORE_LOCAL_INT = 0x14,
        MAKE_ARRAY  = 0x15, // creates array from N values on stack
        ARRAY_GET   = 0x16, // gets element at index
        ARRAY_SET   = 0x17, // sets element at index
        ARRAY_PUSH  = 0x18, // appends value to array
        ARRAY_LEN   = 0x19, // pushes array length onto stack
        HALT = 0xFF
    }
}