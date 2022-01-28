package com.cuno.jsonscript.common;

import org.json.*;
import java.io.*;
import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.InputMismatchException;
import java.util.List;
import java.util.Scanner;

public class FileHelper {

    public static List<String> ValidAccessModifiers = new ArrayList<>();

    public static FileClassSettings DeserializeClass(String filePath) {
        FileClassSettings classSettings = new FileClassSettings();
        try {
            File file = new File(filePath);
            Scanner myReader = new Scanner(file);
            String data = "";
            while (myReader.hasNextLine()) {
                data += myReader.nextLine();
            }
            JSONObject obj = new JSONObject(data);
            classSettings.Name = obj.getString("Name");
            if (classSettings.Name == null) {
                throw new IllegalArgumentException("A name must be provided for class in file \"" + file.getName() + "\"");
            }
            classSettings.Namespace = obj.getString("Namespace");
            if (classSettings.Namespace == null) {
                throw new IllegalArgumentException("A namespace must be provided for class in file \"" + file.getName() + "\"");
            }
            classSettings.AccessModifier = obj.getString("AccessModifier").toLowerCase();
            if (!ValidAccessModifiers.contains(classSettings.AccessModifier)) {
                throw new IllegalArgumentException(String.format("Provided access modifier \"%s\" is not a valid access modifier (class %s in %s)", classSettings.AccessModifier, classSettings.Name, classSettings.Namespace));
            }
            classSettings.Imports = ((JSONArray) obj.get("Imports")).toList();
        }
        catch (FileNotFoundException e) {
            throw new RuntimeException(String.format("Failed to read file %s: Could not find file.", filePath));
        }
        return classSettings;
    }

    public static FileMethodSettings DeserializeMethod(String filePath) {
        FileMethodSettings methodSettings = new FileMethodSettings();
        try {
            File file = new File(filePath);
            Scanner myReader = new Scanner(file);
            String data = "";
            while (myReader.hasNextLine()) {
                data += myReader.nextLine();
            }
            JSONObject obj = new JSONObject(data);
            try { //greatest library
                methodSettings.Name = obj.getString("Name");
                methodSettings.ContainingClass = obj.getString("ContainingClass");
                methodSettings.AccessModifier = obj.getString("AccessModifier").toLowerCase();
                if (!ValidAccessModifiers.contains(methodSettings.AccessModifier)) {
                    throw new IllegalArgumentException(String.format("Provided access modifier \"%s\" is not a valid access modifier (method %s in %s)", methodSettings.AccessModifier, methodSettings.Name, methodSettings.ContainingClass));
                }
                methodSettings.Code = obj.getString("Code");
            }
            catch (JSONException e) {
                if (e.getMessage().contains("[\"Name\"]")) {
                    throw new IllegalArgumentException("A name must be provided for method in file \"" + file.getName() + "\"");
                }
                if (e.getMessage().contains("[\"ContainingClass\"]")) {
                    throw new IllegalArgumentException("A container class must be provided for method in file \"" + file.getName() + "\"");
                }
                if (e.getMessage().contains("[\"AccessModifier\"]")) {
                    throw new IllegalArgumentException("An access modifier must be provided for method in file \"" + file.getName() + "\"");
                }
                if (e.getMessage().contains("[\"Code\"]")) {
                    throw new IllegalArgumentException("Code to run upon method invocation must be provided for method in file \"" + file.getName() + "\"");
                }
            }

            try {
                methodSettings.IsStatic = obj.getBoolean("IsStatic");
            }
            catch (JSONException e) {
                methodSettings.IsStatic = false;
            }

            try {
                methodSettings.ReturnType = obj.getString("ReturnType");
            }
            catch (JSONException e) {
                methodSettings.ReturnType = "void";
            }
            try {
                methodSettings.ParameterNames = ((JSONArray) obj.get("ParameterNames")).toList();
                methodSettings.ParameterTypes = ((JSONArray) obj.get("ParameterTypes")).toList();
            }
            catch (JSONException e) {
                methodSettings.ParameterNames = new ArrayList<>();
                methodSettings.ParameterTypes = new ArrayList<>();
            }
            if (methodSettings.ParameterTypes.size() != methodSettings.ParameterNames.size()) {
                throw new InputMismatchException(String.format("Mismatch in size of ParameterNames and ParameterTypes: both arrays must have the same amount of values (method %s in %s)", methodSettings.Name, methodSettings.ContainingClass));
            }
        }
        catch (FileNotFoundException e) {
            throw new RuntimeException(String.format("Failed to read file %s: Could not find file.", filePath));
        }
        return methodSettings;
    }

    public static FileCompilerSettings DeserializeCompiler(String filePath) {
        FileCompilerSettings compilerSettings = new FileCompilerSettings();
        try {
            File file = new File(filePath);
            Scanner myReader = new Scanner(file);
            String data = "";
            while (myReader.hasNextLine()) {
                data += myReader.nextLine();
            }
            JSONObject obj = new JSONObject(data);
            compilerSettings.SilentCompilation = obj.getBoolean("SilentCompilation");
        }
        catch (FileNotFoundException e) {
            throw new RuntimeException(String.format("Failed to read file %s: Could not find file.", filePath));
        }
        return compilerSettings;
    }

    public static void QuickWrite(String filePath, String contents) {
        try {
            FileWriter writer = new FileWriter(filePath);
            writer.write(contents);
            writer.close();
        }
        catch (IOException e) {
            throw new RuntimeException(String.format("Failed to write to file %s: %s", filePath, e.getMessage()));
        }
    }

    public static class FileClassSettings {
        public String Name;

        public String AccessModifier;

        public String Namespace;

        public List<Object> Imports;
    }

    public static class FileMethodSettings {
        public String Name;

        public String AccessModifier;

        public String ContainingClass;

        public String ReturnType = "void";

        public boolean IsStatic;

        public List<Object> ParameterNames;

        public List<Object> ParameterTypes;

        public String Code;
    }

    public static class FileCompilerSettings {
        public boolean SilentCompilation;
    }
}
