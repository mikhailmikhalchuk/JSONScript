using System;
using System.IO;
using System.Collections.Generic;
using JSONScript.VM;
using JSONScript.Runtime;
using JSONScript.Compiler;

namespace JSONScript
{
    class Program
    {
        public static bool Debug = false;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  jsonscript run <file.json> [--debug]");
                Console.WriteLine("  jsonscript run <directory> --entry Namespace.main [--debug]");
                Console.WriteLine("  jsonscript build <file.json|directory> [--entry Namespace.main]");
                Console.WriteLine("  jsonscript exec <file.jsc> --entry Namespace.main [--debug]");
                return;
            }

            string command = args[0];
            string path = args[1];
            Debug = Array.Exists(args, a => a == "--debug");

            string entryPoint = "Main.main";
            int entryIdx = Array.IndexOf(args, "--entry");

            if (entryIdx >= 0 && entryIdx + 1 < args.Length)
                entryPoint = args[entryIdx + 1];

            switch (command)
            {
                case "run":
                {
                    var table = CompilePath(path);
                    if (Debug)
                    {
                        foreach (var fn in table.Values)
                        {
                            Console.WriteLine($"[{fn.FullName}] Bytecode: {string.Join(", ", fn.Bytecode)}");
                        }
                    }
                    var vm = new VM.VM(table, entryPoint);
                    vm.Run();
                    break;
                }

                case "build":
                {
                    var table = CompilePath(path);
                    string outPath = Directory.Exists(path) ? Path.Combine(path, "output.jsc") : Path.ChangeExtension(path, ".jsc");
                    BuildOutput.SaveTable(outPath, table);
                    Console.WriteLine($"Built: {outPath}");
                    break;
                }

                case "exec":
                {
                    var table = BuildOutput.LoadTable(path);
                    var vm = new VM.VM(table, entryPoint);
                    vm.Run();
                    break;
                }

                default:
                    Console.WriteLine($"Unknown command '{command}'");
                    break;
            }
        }

        static Dictionary<string, CompiledFunction> CompilePath(string path)
        {
            var compiler = new Compiler.Compiler();
            var files = Directory.Exists(path) ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).ToList() : new List<string> { path };

            // First pass - register all signatures
            foreach (var file in files)
            {
                compiler.RegisterFile(file);
            }

            // Stop here if any files were malformed
            compiler.FlushDiagnostics();

            // Second pass - compile all bodies
            foreach (var file in files)
            {
                compiler.CompileFile(file);
            }

            compiler.FlushDiagnostics();
            return compiler.FunctionTable;
        }
    }
}