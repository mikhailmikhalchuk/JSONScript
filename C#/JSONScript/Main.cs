using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Windows.Forms;
using JSONScript.Common;
using System.Diagnostics;

namespace JSONScript
{
    public class Main
    {
        public static readonly string FilesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSONScript", "C#");

        public static readonly List<string> ValidAccessModifiers = new List<string>()
        {
            "public",
            "private",
            "protected",
            "internal",
            "none"
        };

        public void Run()
        {
            FileCompilerSettings compilerSettings = new FileCompilerSettings()
            {
                EntryMethod = "Main",
                EntryNamespace = "ExampleProject.Program",
                AssemblyName = "ExampleAssembly",
                SilentCompilation = false,
                VisualizeOnError = true
            };

            if (!compilerSettings.SilentCompilation)
                Console.WriteLine("Starting compiler service.");

            if (!Directory.Exists(FilesDirectory))
            {
                Console.WriteLine("Main directory does not exist, creating...");
                Directory.CreateDirectory(FilesDirectory + Path.DirectorySeparatorChar + "Compiler");
                Console.WriteLine("Adding compilerSettings.json and example files...");
                string serialized = JsonSerializer.Serialize(compilerSettings, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "compilerSettings.json", serialized);

                FileClassSettings classExample = new FileClassSettings()
                {
                    AccessModifier = "none",
                    IsStatic = false,
                    Name = "Program",
                    Namespace = "ExampleProject",
                    Implements = new List<string>()
                    {
                        "System",
                        "System.Threading"
                    }
                };

                FileMethodSettings methodExample = new FileMethodSettings()
                {
                    AccessModifier = "public",
                    Name = "Main",
                    Namespace = "ExampleProject.Program",
                    ReturnType = "System.Void",
                    IsStatic = false,
                    Code = "ExampleMethod(\"exampleField.ToString()\")"
                };

                FileMethodSettings methodExample2 = new FileMethodSettings()
                {
                    AccessModifier = "private",
                    Name = "ExampleMethod",
                    Namespace = "ExampleProject.Program",
                    ReturnType = "System.Int32",
                    IsStatic = true,
                    ParameterTypes = new List<string>()
                    {
                        "System.String"
                    },
                    ParameterNames = new List<string>()
                    {
                        "exParam"
                    },
                    ParameterDefaultValues = new List<string>()
                    {
                        "\"test\""
                    },
                    Code = "Console.WriteLine(\"Waiting 50 seconds...\");\nThread.Sleep(50000);\nreturn 3"
                };

                FileNamespaceSettings exampleNamespace = new FileNamespaceSettings()
                {
                    Namespace = "ExampleProject",
                    Implements = new List<string>()
                    {
                        "System",
                        "System.Threading"
                    }
                };

                FileFieldSettings exampleField = new FileFieldSettings()
                {
                    Name = "exampleField",
                    IsStatic = true,
                    AccessModifier = "internal",
                    DeclarationValue = "1",
                    Namespace = "ExampleProject.Program",
                    Type = "System.Int32"
                };

                serialized = JsonSerializer.Serialize(methodExample, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "Method-Main.json", serialized);

                serialized = JsonSerializer.Serialize(methodExample2, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "Method-Example.json", serialized);

                serialized = JsonSerializer.Serialize(exampleNamespace, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "Namespace-ExampleProject.json", serialized);

                serialized = JsonSerializer.Serialize(classExample, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "Class-Program.json", serialized);

                serialized = JsonSerializer.Serialize(exampleField, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "Field-ExampleField.json", serialized);

                Console.WriteLine("Done!");
                Process.Start(FilesDirectory);
                return;
            }

            List<CodeMemberMethod> AllMethods = new List<CodeMemberMethod>();
            List<FileMethodSettings> AllMethodsFromDeserialized = new List<FileMethodSettings>();
            List<CodeTypeDeclaration> AllClasses = new List<CodeTypeDeclaration>();
            List<FileClassSettings> AllClassesFromDeserialized = new List<FileClassSettings>();
            List<CodeNamespace> AllNamespaces = new List<CodeNamespace>();
            List<CodeMemberField> AllFields = new List<CodeMemberField>();
            List<FileFieldSettings> AllFieldsFromDeserialized = new List<FileFieldSettings>();

            if (File.Exists(FilesDirectory + Path.DirectorySeparatorChar + "compilerSettings.json"))
            {
                compilerSettings = JsonSerializer.Deserialize<FileCompilerSettings>(File.ReadAllText(FilesDirectory + Path.DirectorySeparatorChar + "compilerSettings.json"));
                if (compilerSettings.EntryNamespace == null)
                {
                    throw new ArgumentException("A valid entry namespace must be specified inside compilerSettings.json.");
                }
                if (compilerSettings.EntryMethod == null)
                {
                    throw new ArgumentException("A valid entry method must be specified inside compilerSettings.json.");
                }
            }
            else
            {
                string serializedSettings = JsonSerializer.Serialize(compilerSettings);
                File.WriteAllText(FilesDirectory + Path.DirectorySeparatorChar + "compilerSettings.json", serializedSettings);
            }

            if (!compilerSettings.SilentCompilation)
                Console.WriteLine("Looking for JSON files.");

            int ignoredFiles = 0;

            foreach (string file in Directory.GetFiles(FilesDirectory))
            {
                if (file.ToLower().Contains("method"))
                {
                    FileMethodSettings deserializedMethod = FileDeserializer.DeserializeMethod(file, out MemberAttributes attributes);
                    CodeMemberMethod memberMethod = new CodeMemberMethod
                    {
                        Name = deserializedMethod.Name,
                        Attributes = attributes,
                        ReturnType = new CodeTypeReference(deserializedMethod.ReturnType)
                    };
                    int ind = 0;
                    foreach (var par in deserializedMethod.ParameterNames)
                    {
                        if (deserializedMethod.ParameterDefaultValues.Count > 0)
                        {
                            memberMethod.Parameters.Add(new CodeParameterDeclarationExpression(deserializedMethod.ParameterTypes[ind], deserializedMethod.ParameterDefaultValues?[ind] != null ? deserializedMethod.ParameterNames[ind] + " = " + deserializedMethod.ParameterDefaultValues?[ind] : deserializedMethod.ParameterNames[ind]));
                        }
                        else
                        {
                            memberMethod.Parameters.Add(new CodeParameterDeclarationExpression(deserializedMethod.ParameterTypes[ind], deserializedMethod.ParameterNames[ind]));
                        }
                        ind++;
                    }
                    memberMethod.Statements.Add(new CodeSnippetExpression(deserializedMethod.Code));
                    AllMethods.Add(memberMethod);
                    AllMethodsFromDeserialized.Add(deserializedMethod);
                }
                else if (file.ToLower().Contains("class"))
                {
                    FileClassSettings deserializedClass = FileDeserializer.DeserializeClass(file, out MemberAttributes attributes);
                    CodeTypeDeclaration memberClass = new CodeTypeDeclaration(deserializedClass.Name)
                    {
                        Attributes = attributes
                    };
                    memberClass.Comments.Add(new CodeCommentStatement(deserializedClass.Namespace));
                    //memberClass.Members.Add(method);
                    AllClasses.Add(memberClass);
                    AllClassesFromDeserialized.Add(deserializedClass);
                }
                else if (file.ToLower().Contains("namespace"))
                {
                    FileNamespaceSettings deserializedNamespace = FileDeserializer.DeserializeNamespace(file);
                    CodeNamespace memberNamespace = new CodeNamespace(deserializedNamespace.Namespace);
                    AllNamespaces.Add(memberNamespace);
                }
                else if (file.ToLower().Contains("field"))
                {
                    FileFieldSettings deserializedField = FileDeserializer.DeserializeField(file, out MemberAttributes attributes);
                    CodeMemberField memberField = new CodeMemberField(deserializedField.Type, deserializedField.Name);
                    memberField.Attributes = attributes;
                    memberField.InitExpression = new CodeSnippetExpression(deserializedField.DeclarationValue);
                    AllFields.Add(memberField);
                    AllFieldsFromDeserialized.Add(deserializedField);
                }
                else if (!file.ToLower().Contains("compilersettings"))
                {
                    ignoredFiles++;
                }
            }

            if (!compilerSettings.SilentCompilation)
            {
                Console.WriteLine($"Found {AllClasses.Count} class{(AllClasses.Count != 1 ? "es" : "")}.");
                Console.WriteLine($"Found {AllMethods.Count} method{(AllMethods.Count != 1 ? "s" : "")}.");
                Console.WriteLine($"Found {AllNamespaces.Count} namespace{(AllNamespaces.Count != 1 ? "s" : "")}.");
                Console.WriteLine($"({ignoredFiles} file{(ignoredFiles != 1 ? "s" : "")} ignored)");
                Console.WriteLine("Assigning methods to classes...");
            }

            int ignoredMethods = 0;
            int index = 0;
            foreach (FileClassSettings cls in AllClassesFromDeserialized)
            {
                List<FileMethodSettings> methodsDes = AllMethodsFromDeserialized.FindAll(m => m.Namespace == cls.Namespace + "." + cls.Name);
                int innerInd = 0;
                bool added = false;
                foreach (FileMethodSettings meth in AllMethodsFromDeserialized)
                {
                    foreach (FileMethodSettings otherM in methodsDes) //best metroid game
                    {
                        if (meth.Equals(otherM))
                        {
                            AllClasses[index].Members.Add(AllMethods[innerInd]);
                            added = true;
                        }
                    }
                    innerInd++;
                }
                if (!added)
                    ignoredMethods++;
            }

            if (!compilerSettings.SilentCompilation)
            {
                Console.WriteLine($"Done! ({ignoredMethods} method{(ignoredMethods != 1 ? "s" : "")} ignored)");
                Console.WriteLine("Assigning fields to classes...");
            }

            int ignoredFields = 0;
            index = 0;
            foreach (FileFieldSettings fld in AllFieldsFromDeserialized)
            {
                List<FileFieldSettings> fieldsDes = AllFieldsFromDeserialized.FindAll(f => f.Namespace == fld.Namespace);
                int innerInd = 0;
                bool added = false;
                foreach (FileFieldSettings feld in AllFieldsFromDeserialized)
                {
                    foreach (FileFieldSettings otherF in fieldsDes)
                    {
                        if (feld.Equals(otherF))
                        {
                            AllClasses[index].Members.Add(AllFields[innerInd]);
                            added = true;
                        }
                    }
                    innerInd++;
                }
                if (!added)
                    ignoredFields++;
            }

            if (!compilerSettings.SilentCompilation)
            {
                Console.WriteLine($"Done! ({ignoredFields} field{(ignoredFields != 1 ? "s" : "")} ignored)");
                Console.WriteLine("Adding classes to namespaces...");
            }

            index = 0;
            foreach (CodeNamespace ns in AllNamespaces.ToList())
            {
                List<CodeTypeDeclaration> settings = AllClasses.FindAll(c => c.Comments[0].Comment.Text == ns.Name);
                List<FileClassSettings> settingsDes = AllClassesFromDeserialized.FindAll(c => c.Namespace == ns.Name);
                int innerInd = 0;
                foreach (CodeTypeDeclaration cls in settings)
                {
                    AllNamespaces[index].Types.Add(cls);
                    foreach (string implUsing in settingsDes[innerInd].Implements)
                    {
                        AllNamespaces[index].Imports.Add(new CodeNamespaceImport(implUsing));
                    }
                    innerInd++;
                }
                index++;
            }

            if (!compilerSettings.SilentCompilation)
                Console.WriteLine("Creating the compiler...");

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            foreach (CodeNamespace ns in AllNamespaces)
            {
                compileUnit.Namespaces.Add(ns);
            }

            List<string> totalUsings = new List<string>();
            foreach (FileClassSettings cs in AllClassesFromDeserialized)
            {
                foreach (string reference in cs.Implements)
                {
                    totalUsings.Add(reference + ".dll");
                }
            }
            CompilerParameters compilerParameters = new CompilerParameters(totalUsings.ToArray())
            {
                GenerateInMemory = true,
                OutputAssembly = compilerSettings.AssemblyName,
                TempFiles = new TempFileCollection(FilesDirectory + Path.DirectorySeparatorChar + "Compiler")
            };

            if (!compilerSettings.SilentCompilation)
                Console.WriteLine("Creating and running assembly...");

            CompilerResults compilerResults = new CSharpCodeProvider().CompileAssemblyFromDom(compilerParameters, compileUnit);
            if (compilerResults.Errors.HasErrors)
            {
                foreach (CompilerError error in compilerResults.Errors)
                {
                    Console.WriteLine(error.ToString());
                }
                if (compilerSettings.VisualizeOnError)
                {
                    Console.WriteLine("Generated visual for debugging:");

                    StringWriter textWriter = new StringWriter();
                    new CSharpCodeProvider().GenerateCodeFromCompileUnit(compileUnit, textWriter, new CodeGeneratorOptions() { BracingStyle = "C" });
                    Console.WriteLine(textWriter.ToString());
                }

                throw new Exception("Errors occurred while compiling the JSON. Check the console for details.");
            }

            object assemblyInstance;
            try
            {
                assemblyInstance = compilerResults.CompiledAssembly.CreateInstance(compilerSettings.EntryNamespace);
            }
            catch (IOException)
            {
                throw new Exception("Errors occurred while compiling the JSON. Check the console for details.");
            }

            if (assemblyInstance == null)
            {
                throw new ArgumentException("The entry namespace was not found. Make sure it is valid, and contains only the namespace as well as the class name.");
            }

            MethodInfo info = assemblyInstance.GetType().GetMethod(compilerSettings.EntryMethod);

            if (info == null)
            {
                throw new ArgumentException("The entry method was not found. Make sure it is valid, only contains the method name, and is marked public.");
            }
            if (info.GetParameters().Length > 0)
            {
                throw new Exception("The entry method must not contain any required arguments.");
            }

            if (!compilerSettings.SilentCompilation)
            {
                Console.WriteLine("Done!\n--------");
            }

            object result = info.Invoke(assemblyInstance, null);
            //Console.WriteLine(containerNamespace.Name + deserialized2.Name);
        }
    }
}
