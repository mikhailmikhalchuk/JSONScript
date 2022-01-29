import json, os, sys
from pathlib import Path

FilePath = os.path.expanduser("~/Documents") + "/JSONScript/Python"

if (not Path(FilePath).exists()):
    print("Provided directory does not exist, creating...")
    os.mkdir(FilePath)
    print("Created!")

CompilerSettings = {"silentStartup": "false"}

print("Validating compilerSettings.json...")
try:
    with open(FilePath + "/compilerSettings.json", 'r') as f:
        CompilerSettings = json.load(f)
except:
    print("compilerSettings.json does not exist, creating...")
    with open(FilePath + "/compilerSettings.json", "w") as f:
        json.dump(CompilerSettings, f, indent=4)

if (CompilerSettings["silentStartup"] == "false"):
    print("Checking main.json...")
try:
    with open(FilePath + "/main.json", 'r') as f:
        if (CompilerSettings["silentStartup"] == "false"):
            print("Fetching main.json...")
        data = json.load(f)
        if (CompilerSettings["silentStartup"] == "false"):
            print("Running main.json...")
        exec(data["code"])

except:
    if sys.exc_info()[0] == KeyError:
        print("The code parameter was not found in main.json. Ensure the parameter was added.")
    elif sys.exc_info()[0] == SyntaxError:
        print(f"An exception occurred while running the code. Ensure that it is valid.")
    else:
        print("Could not find main.json. Ensure the file is valid and placed in the correct directory.")