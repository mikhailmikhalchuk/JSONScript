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
                compilerJson.put("EntryClass", "Program");
                compilerJson.put("SilentCompilation", false);
                compilerJson.put("DeleteAfterRun", true);
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

        List<String> masterCode = new ArrayList<>();
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

            String masterImports = "";
            for (int k = 0; k < settings.Imports.size(); k++) {
                masterImports += "\nimport " + settings.Imports.get(k) + ";";
            }

            masterCode.add(String.format("""
                    package %s;
                    
                    %s
                    
                    \n
                    %s class %s {
                        %s
                    }
                    """, settings.Namespace, masterImports, settings.AccessModifier != "none" ? settings.AccessModifier : "", settings.Name, masterMethods));
            //System.out.println(masterCode);
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println(String.format("Done! (%s method%s ignored)", AllMethods.size() - nonIgnoredMethods, AllMethods.size() - nonIgnoredMethods != 1 ? "s were" : " was"));
            System.out.println(String.format("----\nWriting program to temporary file%s...", AllClasses.size() != 1 ? "s" : ""));
        }

        for (int i = 0; i < AllClasses.size(); i++) {
            FileHelper.QuickWrite(FilesDirectory + String.format("/Compiler/%s.java", AllClasses.get(i).Name), masterCode.get(i));
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("----\nRunning compiler...");
        }

        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

        boolean success = true;
        for (int i = AllClasses.size(); i > AllClasses.size(); i--) {
            success = compiler.run(null, System.out, null, FilesDirectory + String.format("/Compiler/%s.java", AllClasses.get(i).Name)) == 0;
        }

        if (success) {
            if (!compilerSettings.SilentCompilation) {
                System.out.println("Done!");
                System.out.println("----\nRunning virtual machine...\n--------");
            }
            Runtime rt = Runtime.getRuntime();
            try {
                Process pr = rt.exec("java " + FilesDirectory + String.format("/Compiler/%s.java", compilerSettings.EntryClass));
                BufferedReader input = new BufferedReader(new InputStreamReader(pr.getInputStream()));

                String outLine;
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
                throw new RuntimeException("Running virtual machine failed: " + e.getMessage());
            }
        }
        if (!compilerSettings.DeleteAfterRun) return;
        folder = new File(FilesDirectory + "/Compiler");
        File[] tempFiles = folder.listFiles();
        for (int i = 0; i < tempFiles.length; i++) {
            if (!tempFiles[i].delete()) {
                System.err.println("Failed to delete temporary compiler files");
            }
        }
    }
}
