# JSONScript

## Usage
Clone the repo to any directory. Build the program, which will generate a `compilerSettings.json` file inside the specified directory, as well as some example files.

JSON files containing code are added to this directory to be read by the program. You **MUST** prefix each file with either `Class` or `Method`, otherwise they will be **IGNORED!!**

## File structure
### Class
- `AccessModifier` (String) - the access modifier for the class. Valid options are: `private`, `public`, `protected`, or `none`.

- `Name` (String) - the name of the class.

- `Namespace` (String) - the namespace of the class, excluding the class name.

- `Imports` (List\<String\>) - a list of packages to import. Only include the package name (e.g. `java.util.List`, `java.io.*`).

### Method
- `AccessModifier` (String) - the access modifier for the method. Valid options are: `private`, `public`, `protected`, or `none`.

- `ReturnType` (String) - the method's return type. Default `void`.

- `IsStatic` (boolean) - whether or not the method is `static`. Default `false`.

- `Name` (String) - the name of the method.

- `ContainerClass` (String) - the class containing the method.

- `ParameterTypes` (List\<String\>) - a list of parameter types to include.

- `ParameterNames` (List\<String\>) - a list of parameter names to associate types with.

- `Code` (String) - the code to run upon invocation of the method.

### Compiler settings
- `SilentCompilation` (boolean) - whether or not to report compilation status and progress to the terminal.
