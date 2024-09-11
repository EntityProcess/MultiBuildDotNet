
# MultiDotSolutionBuild

**MultiDotSolutionBuild** is a .NET console app designed to sequentially or concurrently build multiple `.NET` solutions based on a configuration file. The app supports resolving dependencies between solutions and builds them accordingly, either in sequence or in parallel where possible.

## Features

- Build multiple .NET solutions in a specified order or concurrently if possible.
- Resolves dependencies between solutions.
- Generates a build order and uses it to control the build process.
- Supports customizable build commands using placeholders for solution paths.
- Stops the build process if any command fails and displays the name of the solution that failed.
- Supports both `dotnet` CLI and Visual Studio (`devenv.com`) build commands.

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/MultiDotSolutionBuild.git
   cd MultiDotSolutionBuild
   ```

2. Build the console app:

   ```bash
   dotnet build
   ```

## Usage

1. **Dependency Config File (`solutions.json`)**

   Create a `solutions.json` file that contains the list of solutions with their dependencies. This file will be used to generate the build order and determine which solutions can be built simultaneously.

   Example `solutions.json`:
   ```json
   {
     "Solutions": [
       {
         "Path": "path/to/solution1.sln",
         "DependsOn": []
       },
       {
         "Path": "path/to/solution2.sln",
         "DependsOn": ["path/to/solution1.sln"]
       },
       {
         "Path": "path/to/solution3.sln",
         "DependsOn": ["path/to/solution1.sln"]
       },
       {
         "Path": "path/to/solution4.sln",
         "DependsOn": ["path/to/solution2.sln", "path/to/solution3.sln"]
       }
     ]
   }
   ```

2. **Generating the Build Order**

   The app will generate a `build-order.json` file containing the order in which solutions should be built, including any that can be built simultaneously.

   Run the app:

   ```bash
   dotnet run
   ```

   Example output of `build-order.json`:
   ```json
   {
     "BuildGroups": [
       ["path/to/solution1.sln"],
       ["path/to/solution2.sln", "path/to/solution3.sln"],
       ["path/to/solution4.sln"]
     ]
   }
   ```

3. **Build Process**

   After generating the build order, the app will proceed to build the solutions. Solutions in the same group will be built simultaneously, while dependent solutions will be built in sequence.

4. **Command Template**

   You can also specify custom commands in the `CommandTemplate` using the `{solution}` placeholder in the `config.json`. For example:

   Example `config.json`:
   ```json
   {
     "CommandTemplate": "dotnet build {solution}",
     "Solutions": [
       "path/to/solution1.sln",
       "path/to/solution2.sln"
     ]
   }
   ```

## Example Output

```
Running command: dotnet build path/to/solution1.sln
Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
...

Running command: dotnet build path/to/solution2.sln
Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
...
Build failed for solution: path/to/solution2.sln
```

## Configuration Files

### solutions.json

This file should include:

- `Path`: The path to the `.sln` file.
- `DependsOn`: A list of `.sln` file paths that the solution depends on.

### config.json

This file should include:

- `CommandTemplate`: The command to execute for each solution, with `{solution}` as the placeholder for the solution file path.
- `Solutions`: A list of `.sln` file paths to build.

## Requirements

- .NET SDK (5.0 or later)
- Visual Studio (if using `devenv.com` command)
- Command-line access

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributions

Contributions are welcome! Feel free to open issues or submit pull requests for improvements and bug fixes.