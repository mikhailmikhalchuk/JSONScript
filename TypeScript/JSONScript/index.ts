import fs from "fs";


//---------
var FilePath = "[Your path]"
//---------

interface CompilerSettings {
    silentCompilation: boolean;
}

interface Main {
    code: string;
}

if (FilePath == "[Your path]") {
    console.error("Please set the FilePath variable to the directory in which to read and compile.");
}
else {
    fs.access(FilePath, function(err) {
        if (err) {
            console.log("Provided directory does not exist, creating...");
            fs.mkdir(FilePath, function (err) {
                if (err) {
                    console.error("Error occurred while creating directory: " + err);
                    return;
                }
                else {
                    console.log("Created!");
                }
            });
        }
    });
    
    fs.access(FilePath + "/compilerSettings.json", function (err) {
        if (err) {
            console.log("compilerSettings.json does not exist, creating...");
            var write: CompilerSettings = {
                "silentCompilation": false
            };
            var jsonwr = JSON.stringify(write, null, "\t");
            fs.writeFile(FilePath + "/compilerSettings.json", jsonwr, function (err) {
                console.log("Done!");
                fs.readdir(FilePath, function (err, files) {
                    var compilerSettings: CompilerSettings = require(FilePath + "/compilerSettings.json");
                    if (!compilerSettings.silentCompilation)
                        console.log("Fetching files...");
                    files.forEach((f) => {
                        
                    });
                    if (!compilerSettings.silentCompilation)
                        console.log("Checking main.json...");
                    fs.access(FilePath + "/main.json", function (err) {
                        if (err) {
                            console.error("Could not find main.json. Ensure the file is valid and placed in the correct directory.");
                            return;
                        }
                        else {
                            if (!compilerSettings.silentCompilation)
                                console.log("Fetching main.json...");
                            fs.readFile(FilePath + "/main.json", 'utf-8', function (err, data) {
                                if (!compilerSettings.silentCompilation)
                                    console.log("Done!\n-------");
                                eval((JSON.parse(data) as Main).code);
                            });
                        }
                    });
                });
            });
        }
        else {
            fs.readdir(FilePath, function (err, files) {
                var compilerSettings: CompilerSettings = require(FilePath + "/compilerSettings.json");
                if (!compilerSettings.silentCompilation)
                    console.log("Fetching files...");
                files.forEach((f) => {
                    
                });
                if (!compilerSettings.silentCompilation)
                    console.log("Checking main.json...");
                fs.access(FilePath + "/main.json", function (err) {
                    if (err) {
                        console.error("Could not find main.json. Ensure the file is valid and placed in the correct directory.");
                        return;
                    }
                    else {
                        if (!compilerSettings.silentCompilation)
                            console.log("Fetching main.json...");
                        fs.readFile(FilePath + "/main.json", 'utf-8', function (err, data) {
                            if (!compilerSettings.silentCompilation)
                                console.log("Done!\n-------");
                            eval((JSON.parse(data) as Main).code);
                        });
                    }
                });
            });
        }
    });
}