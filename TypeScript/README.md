# JSONScript

## Usage
Clone this repo to any directory. Run `npm install` in that directory to install the required packages

Open `index.ts`, and change `FilePath` to the absolute path of the directory in which to read files. Run the program afterwards to create the necessary files.

The main entry file is named `main.json`. All code should be included in the `Code` parameter.

## File structure
### Main
- `code` (string) - the code to run.

### Compiler settings
- `silentCompilation` (boolean) - whether or not to report compilation status and progress to the terminal.