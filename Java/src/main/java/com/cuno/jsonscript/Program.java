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
import java.util.Scanner;

public class Program {
    public static String FilesDirectory = new JFileChooser().getFileSystemView().getDefaultDirectory().toString() + "/JSONScript/Java";

    public static void main(String[] args) {
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
                System.out.println("Done!");
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

        String masterCode = """
                package com.compiler.jsonscript;
                
                public class Program {
                    public static void main(String[] args) {
                        System.out.println("Test");
                    }
                }
                """;
        File folder = new File(FilesDirectory);
        File[] jsonList = folder.listFiles();

        for (int i = 0; i < jsonList.length; i++) {
            File file = jsonList[i];
            if (!file.isFile()) {
                continue;
            }
            else {
                if (file.getName().toLowerCase().contains("Method")) {

                }
            }
        }

        if (!compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("Writing program to temporary file...");
        }

        FileHelper.QuickWrite(FilesDirectory + "/Compiler/Program.java", masterCode);

        if (!compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("Running compiler...");
        }

        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

        int success = compiler.run(null, System.out, null, FilesDirectory + "/Compiler/Program.java");

        if (success == 0 && !compilerSettings.SilentCompilation) {
            System.out.println("Done!");
            System.out.println("Running compiled file...\n--------");
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

        /*try {
            File file = new File("C:/Users/Cuno/Documents/JSONScript/Java/amogus.json");
            Scanner myReader = new Scanner(file);
            String data = "";
            while (myReader.hasNextLine()) {
                data += myReader.nextLine();
            }
            JSONObject obj = new JSONObject(data);
            String test = obj.getString("AccessModifier");
            System.out.println(test);
            JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();
            String fileToCompile = "sus";
            //Object sus = compiler.run(null, null, null, fileToCompile);
        }
        catch (FileNotFoundException e) {
            System.out.println("Bad!");
        }*/
    }
}
