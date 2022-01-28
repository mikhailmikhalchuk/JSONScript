package com.cuno.jsonscript;

import com.cuno.jsonscript.common.FileHelper;
import com.cuno.jsonscript.common.FileHelper.*;
import org.json.*;

import javax.swing.*;
import javax.tools.JavaCompiler;
import javax.tools.ToolProvider;
import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;

public class Program {
    public static String FilesDirectory = new JFileChooser().getFileSystemView().getDefaultDirectory().toString() + "/JSONScript/Java";

    public static void main(String[] args) {
        FileHelper.ValidAccessModifiers.add("public");
        FileHelper.ValidAccessModifiers.add("private");
        FileHelper.ValidAccessModifiers.add("protected");
        FileHelper.ValidAccessModifiers.add("none");
        FileCompilerSettings compilerSettings = new FileCompilerSettings();
        compilerSettings.SilentCompilation = false;
        if (!Files.exists(Path.of(FilesDirectory + "/Compiler"))) {
            System.out.println("Main directory does not exist, creating...");
            try {
                Files.createDirectory(Path.of(FilesDirectory + "/Compiler"));
                System.out.println("Adding compilerSettings.json and example files...");
                JSONObject compilerJson = new JSONObject();
                compilerJson.put("SilentCompilation", false);
                FileHelper.QuickWrite(FilesDirectory + "/compilerSettings.json", compilerJson.toString(2));

                JSONObject exampleClass = new JSONObject();
                List<String> imports = new ArrayList<>();
                imports.add("java.io.*");
                imports.add("java.util.List");
                exampleClass.put("Imports", imports);
                exampleClass.put("AccessModifier", "public");
                exampleClass.put("Namespace", "com.compiler.jsonscript");
                exampleClass.put("Name", "Program");
                FileHelper.QuickWrite(FilesDirectory + "/Class-Program.json", exampleClass.toString(2));

                JSONObject exampleMethod = new JSONObject();
                List<String> paramNames = new ArrayList<>();
                List<String> paramTypes = new ArrayList<>();
                paramNames.add("args");
                paramTypes.add("String[]");
                exampleMethod.put("ParameterTypes", paramTypes);
                exampleMethod.put("ParameterNames", paramNames);
                exampleMethod.put("Code", "System.out.println(\"Testing...\");\nexampleMethod(5, false);");
                exampleMethod.put("IsStatic", true);
                exampleMethod.put("ContainingClass", "Program");
                exampleMethod.put("AccessModifier", "public");
                exampleMethod.put("Name", "main");
                FileHelper.QuickWrite(FilesDirectory + "/Method-Main.json", exampleMethod.toString(2));

                JSONObject exampleMethod2 = new JSONObject();
                List<String> paramNames2 = new ArrayList<>();
                List<String> paramTypes2 = new ArrayList<>();
                paramNames2.add("ex");
                paramTypes2.add("int");
                paramNames2.add("ex2");
                paramTypes2.add("boolean");
                exampleMethod2.put("ParameterTypes", paramTypes2);
                exampleMethod2.put("ParameterNames", paramNames2);
                exampleMethod2.put("Code", "return 3;");
                exampleMethod2.put("IsStatic", true);
                exampleMethod2.put("ReturnType", "int");
                exampleMethod2.put("ContainingClass", "Program");
                exampleMethod2.put("AccessModifier", "private");
                exampleMethod2.put("Name", "exampleMethod");
                FileHelper.QuickWrite(FilesDirectory + "/Method-ExampleMethod.json", exampleMethod2.toString(2));

                System.out.println("Done!");
                return;
            }
            catch (IOException e) {

            }
        }
        else {
            compilerSettings = FileHelper.DeserializeCompiler(FilesDirectory + "/compilerSettings.json");
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println("Starting compiler service.");
            System.out.println("Looking for JSON files.");
        }

        List<FileClassSettings> AllClasses = new ArrayList<>();
        List<FileMethodSettings> AllMethods = new ArrayList<>();
        int ignoredFiles = 0;

        String masterCode = """
                """;
        File folder = new File(FilesDirectory);
        File[] jsonList = folder.listFiles();

        for (int i = 0; i < jsonList.length; i++) {
            File file = jsonList[i];
            if (!file.isFile()) {
                continue;
            }
            else {
                if (file.getName().toLowerCase().contains("class")) {
                    AllClasses.add(FileHelper.DeserializeClass(file.getAbsolutePath()));
                }
                else if (file.getName().toLowerCase().contains("method")) {
                    AllMethods.add(FileHelper.DeserializeMethod(file.getAbsolutePath()));
                }
                else if (!file.getName().equals("compilerSettings.json")) {
                    ignoredFiles++;
                }
            }
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println(String.format("Found %s class%s.", AllClasses.size(), AllClasses.size() != 1 ? "es" : ""));
            System.out.println(String.format("Found %s method%s.", AllMethods.size(), AllMethods.size() != 1 ? "s" : ""));
            System.out.println(String.format("(%s file%s ignored)", ignoredFiles, ignoredFiles != 1 ? "s were" : " was"));
            System.out.println("----\nAssigning methods to classes...");
        }

        int nonIgnoredMethods = 0;

        for (int i = 0; i < AllClasses.size(); i++) {
            FileClassSettings settings = AllClasses.get(i);
            String masterMethods = "";
            for (int j = 0; j < AllMethods.size(); j++) { //most efficient method
                FileMethodSettings childMethod = AllMethods.get(j);
                if (childMethod.ContainingClass.equals(settings.Name)) {
                    nonIgnoredMethods++;
                    String params = "";
                    for (int k = 0; k < childMethod.ParameterNames.size(); k++) { //system.linq
                        if (params == "") {
                            params += childMethod.ParameterTypes.get(k) + " " + childMethod.ParameterNames.get(k);
                        }
                        else {
                            params += ", " + childMethod.ParameterTypes.get(k) + " " + childMethod.ParameterNames.get(k);
                        }
                    }
                    masterMethods += String.format("""
                                %s %s %s %s(%s) {
                                    %s
                                }
                            """, childMethod.AccessModifier != "none" ? childMethod.AccessModifier : "", childMethod.IsStatic ? "static" : "", childMethod.ReturnType, childMethod.Name, params, childMethod.Code);
                }
            }

            masterCode += String.format("""
                    package %s;
                    \n
                    %s class %s {
                        %s
                    }
                    """, settings.Namespace, settings.AccessModifier != "none" ? settings.AccessModifier : "", settings.Name, masterMethods);
            //System.out.println(masterCode);
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println(String.format("Done! (%s method%s ignored)", AllMethods.size() - nonIgnoredMethods, AllMethods.size() - nonIgnoredMethods != 1 ? "s were" : " was"));
            System.out.println("----\nWriting program to temporary file...");
        }

        FileHelper.QuickWrite(FilesDirectory + "/Compiler/Program.java", masterCode);

        if (!compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("----\nRunning compiler...");
        }

        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

        int success = compiler.run(null, System.out, null, FilesDirectory + "/Compiler/Program.java");

        if (success == 0 && !compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("----\nRunning compiled file...\n--------");
            Runtime rt = Runtime.getRuntime();
            try {
                Process pr = rt.exec("java " + FilesDirectory + "/Compiler/Program.java");
                BufferedReader input = new BufferedReader(new InputStreamReader(pr.getInputStream()));

                String outLine = "";
                while ((outLine = input.readLine()) != null) {
                    System.out.println(outLine);
                }

                try {
                    int exitVal = pr.waitFor();
                    System.out.println("Exited with error code " + exitVal);
                }
                catch (InterruptedException e) {
                    throw new RuntimeException("Obtaining exit code failed: " + e.getMessage());
                }
            }
            catch (IOException e) {
                throw new RuntimeException("Running compiled file failed: " + e.getMessage());
            }
        }
    }
}
