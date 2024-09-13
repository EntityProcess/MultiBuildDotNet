
# MultiBuildDotNet

**MultiBuildDotNet** is a .NET console app designed to sequentially build multiple `.NET` solutions based on a configuration file. The app allows users to specify custom commands (e.g., `dotnet build`) to run for each solution and substitutes the solution paths dynamically. It also reads a master build order config file to ensure that solutions are built in the correct order.

## Features

- Build multiple .NET solutions in a specified order.
- Supports customizable build commands using placeholders for solution paths.
- Reads a master build order file to ensure the correct order of solutions.

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/MultiBuildDotNet.git
   cd MultiBuildDotNet
   ```

2. Build the console app:

   ```bash
   dotnet build
   ```

## Usage

1. Create a `config.json` file in the project directory. This file will contain the list of solution file paths and the command template you want to run.

   Example `config.json`:
   ```json
   {
     "CommandTemplate": "dotnet build {solution}",
     "WorkingDirectory": "C:/path/to/your/repo",
     "Solutions": [
       "C:/Projects/Solution1.sln",
       "C:/Projects/Solution2.sln"
     ]
   }
   ```

   - Replace `"dotnet build {solution}"` with the desired command.
   - Use `{solution}` as the placeholder for each solution's path, which will be replaced by the entries in the `Solutions` array.
   - Paths containing backslashes (`\`) in `Solutions` will be converted to forward slashes (`/`).

2. Optionally, create a `masterBuildOrder.json` file in the project directory. This file contains the solutions in the order they should be built:

   Example `masterBuildOrder.json`:
   ```json
   {
     "Solutions": [
       "C:/Projects/Solution1.sln",
       "C:/Projects/Solution2.sln"
     ]
   }
   ```

   If `masterBuildOrder.json` exists, the app will reorder the solutions from `config.json` based on the master build order specified in `masterBuildOrder.json`. Solutions in `config.json` that are not found in the master build order will be appended in their original order.

3. You can specify two Git commits to find solutions that contain changes between those commits. The app will use the changed solutions between the specified commits, but any solutions in `config.json` that do not appear in the list of changed solutions will also be included in the build process.

   Example usage with commit hashes:
   ```bash
   dotnet run <commit1> <commit2>
   ```

   Example:
   ```bash
   dotnet run abc123 def456
   ```

   If no commit arguments are provided, the app will use the existing solutions from `config.json`.

4. Run the app:

   ```bash
   dotnet run
   ```

   The app will execute the command for each solution sequentially. If a command fails, the process will stop, and the name of the failed solution will be displayed.

## Example Output

```
Running command: dotnet build C:/Projects/Solution1.sln
Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
...

Running command: dotnet build C:/Projects/Solution2.sln
Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
...
Build failed for solution: C:/Projects/Solution2.sln
```

## Configuration File

- `config.json`: Contains the command template, working directory, and list of solution paths.
- `masterBuildOrder.json`: Contains the solutions in the order they should be built.

### Example `config.json`:

```json
{
  "CommandTemplate": "dotnet build {solution}",
  "WorkingDirectory": "C:/path/to/your/repo",
  "Solutions": [
    "C:/Projects/Solution1.sln",
    "C:/Projects/Solution2.sln"
  ]
}
```

### Example `masterBuildOrder.json`:

```json
{
  "Solutions": [
    "C:/Projects/Solution2.sln",
    "C:/Projects/Solution1.sln"
  ]
}
```

## Requirements

- .NET SDK (5.0 or later)
- Visual Studio (if using `devenv.com` command)
- Command-line access

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributions

Contributions are welcome! Feel free to open issues or submit pull requests for improvements and bug fixes.