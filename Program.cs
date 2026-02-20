using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using JSONScript.VM;
using JSONScript.Runtime;
using JSONScript.Compiler;
using JSONScript.VM.Graphics;
using JSONScript.VM.Graphics.Metal;
using JSONScript.VM.Graphics.Vulkan;

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
            bool graphics = Array.Exists(args, a => a == "--graphics");

            string device = "metal";
            foreach (var arg in args)
            {
                if (arg.StartsWith("--gldevice:"))
                    device = arg.Split(':')[1].ToLower();
            }

            string entryPoint = "Main.main";
            int entryIdx = Array.IndexOf(args, "--entry");

            if (entryIdx >= 0 && entryIdx + 1 < args.Length)
                entryPoint = args[entryIdx + 1];

            switch (command)
            {
                case "run":
                {
                    var table = CompilePath(path);

                    string[] scriptArgs = args.SkipWhile(a => a != "--").Skip(1).ToArray();
                    var vm = new VM.VM(table, entryPoint, scriptArgs);

                    if (graphics)
                    {
                        IGraphicsBackend backend = device switch
                        {
                            "metal" => new MetalBackend(),
                            "vulkan" => new VulkanBackend(),
                            _ => throw new Exception($"Unknown graphics device '{device}'")
                        };
                        backend.Init(800, 600, "JSONScript");
                        vm.SetGraphicsBackend(backend);  //must be before run
                    }

                    vm.Run();

                    if (graphics)
                    {
                        var gfxBackend = vm.GetGraphicsBackend();
                        if (gfxBackend != null)
                        {
                            gfxBackend.SetEventHandler((eventName, args) => vm.FireEvent(eventName, args));
                            gfxBackend.RunLoop();
                        }
                    }

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
                    string[] scriptArgs = args.SkipWhile(a => a != "--").Skip(1).ToArray();
                    var vm = new VM.VM(table, entryPoint, scriptArgs);

                    if (graphics)
                    {
                        IGraphicsBackend backend = device switch
                        {
                            "metal"  => new MetalBackend(),
                            "vulkan" => new VulkanBackend(),
                            _ => throw new Exception($"Unknown graphics device '{device}'")
                        };
                        backend.Init(800, 600, "JSONScript");
                        vm.SetGraphicsBackend(backend);
                    }

                    vm.Run();

                    if (graphics)
                        vm.GetGraphicsBackend()?.RunLoop();

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

            compiler.RegisterNativeNamespaces();

            foreach (var file in files)
            {
                compiler.RegisterFile(file);
            }

            compiler.FlushDiagnostics();

            foreach (var file in files)
            {
                compiler.CompileFile(file);
            }

            compiler.FlushDiagnostics();
            return compiler.FunctionTable;
        }
    }
}