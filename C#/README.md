# JSONScript

## Usage
Clone the repo to any directory. Build the program, which will generate a `compilerSettings.json` file inside the specified directory, as well as some example files.

JSON files containing code are added to this directory to be read by the program. You **MUST** prefix each file with either `Class`, `Method`, or `Namespace`, otherwise they will be **IGNORED!!**

## File structure
### Class
- `AccessModifier` (string) - the access modifier for the class. Valid options are: `private`, `public`, `internal`, `protected`, or `none`.

- `IsStatic` (boolean) - whether or not the class is `static`. Default `false`.

- `Name` (string) - the name of the class.

- `Namespace` (string) - the namespace of the class, excluding the class name.

- `Implements` (List\<string\>) - a list of namespaces to import. Only include the namespace name (e.g. `System`, `System.IO`).

### Method
- `AccessModifier` (string) - the access modifier for the method. Valid options are: `private`, `public`, `internal`, `protected`, or `none`.

- `ReturnType` (string) - the method's return type. Fully quantify the type (e.g. `System.Int16` for a `short`, `System.Void` for `void`). Default `System.Void`.

- `IsStatic` (boolean) - whether or not the method is `static`. Default `false`.

- `Name` (string) - the name of the method.

- `Namespace` (string) - the namespace of the method, including the containing class.

- `ParameterTypes` (List\<string\>) - a list of parameter types to include. Fully quantify the type (e.g. `System.Int16` for a `short`, `System.Void` for `void`).

- `ParameterNames` (List\<string\>) - a list of parameter names to associate types with.

- `ParameterDefaultValues` (List\<string\>) - a list of default values to assign to parameters.

- `Code` (string) - the code to run upon invocation of the method.

### Namespace
- `Namespace` (string) - the namespace to include.

- `Implements` (List\<string\>) - a list of namespaces to import. Only include the namespace name (e.g. `System`, `System.IO`). This should include all namespaces used by this namespace's classes.

### Compiler settings
- `EntryNamespace` (string) - the namespace to follow for the main method, including the class name but excluding the method name.

- `EntryMethod` (string) - the method to run upon startup. Only include the method name.

- `AssemblyName` (string) - the name to assign the assembly created upon compilation.

- `SilentCompilation` (boolean) - whether or not to report compilation status and progress to the terminal.

- `VisualizeOnError` (boolean) - whether or not to dump visual versions of errors to the terminal.