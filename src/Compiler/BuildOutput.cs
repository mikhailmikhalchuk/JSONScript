using System;
using System.IO;
using System.Collections.Generic;
using JSONScript.Runtime;
using JSONScript.Compiler;

namespace JSONScript.Compiler
{
    public static class BuildOutput
    {
        public static void SaveTable(string path, Dictionary<string, CompiledFunction> table)
        {
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(table.Count);
            foreach (var fn in table.Values)
            {
                writer.Write(fn.FullName);
                writer.Write(fn.LocalCount);
                writer.Write(fn.ParamCount);

                writer.Write(fn.Bytecode.Length);
                writer.Write(fn.Bytecode);

                writer.Write(fn.Constants.Length);
                foreach (var c in fn.Constants)
                {
                    switch (c.Type)
                    {
                        case JSType.INT:
                            writer.Write((byte)0);
                            writer.Write(c.IntValue);
                            break;
                        case JSType.FLOAT:
                            writer.Write((byte)1);
                            writer.Write(c.FloatValue);
                            break;
                        case JSType.STRING:
                            writer.Write((byte)2);
                            writer.Write(c.StringValue!);
                            break;
                        case JSType.BOOL:
                            writer.Write((byte)3);
                            writer.Write(c.BoolValue);
                            break;
                        case JSType.NULL:
                            writer.Write((byte)4);
                            break;
                        default:
                            throw new Exception($"Cannot serialize type {c.Type}");
                    }
                }
            }
        }

        public static Dictionary<string, CompiledFunction> LoadTable(string path)
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            int count = reader.ReadInt32();
            var table = new Dictionary<string, CompiledFunction>();

            for (int f = 0; f < count; f++)
            {
                string fullName = reader.ReadString();
                int localCount = reader.ReadInt32();
                int paramCount = reader.ReadInt32();

                int bytecodeLen = reader.ReadInt32();
                byte[] bytecode = reader.ReadBytes(bytecodeLen);

                int constCount = reader.ReadInt32();
                var constants = new Value[constCount];
                for (int i = 0; i < constCount; i++)
                {
                    byte type = reader.ReadByte();
                    constants[i] = type switch
                    {
                        0 => new Value(reader.ReadInt64()),
                        1 => new Value(reader.ReadDouble()),
                        2 => new Value(reader.ReadString()),
                        3 => new Value(reader.ReadBoolean()),
                        4 => new Value(),
                        _ => throw new Exception($"Unknown type {type}")
                    };
                }

                table[fullName] = new CompiledFunction(fullName, bytecode, constants, localCount, paramCount);
            }

            return table;
        }
    }
}