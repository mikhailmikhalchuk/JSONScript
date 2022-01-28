package com.cuno.jsonscript.common;

import org.json.*;
import java.io.*;
import java.util.Scanner;

public class FileHelper {

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

    public static class FileCompilerSettings {
        public boolean SilentCompilation;
    }
}
