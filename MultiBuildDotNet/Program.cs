using SolutionFinderLib;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        string configFilePath = "config.json"; // Path to your JSON config file
        string masterBuildOrderFilePath = "masterBuildOrder.json"; // Path to master build order config

        // Check if the config.json file exists in the current directory
        if (!File.Exists(configFilePath))
        {
            // Try loading the config.json from the directory where the executable is running
            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            configFilePath = Path.Combine(executableDirectory, "config.json");

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"Error: config.json file not found at the default location or executable directory.");
                return;
            }
        }

        // Output the path of the config.json being used
        Console.WriteLine($"Using config.json from path: {configFilePath}");

        List<string> solutions = new List<string>();

        // Read the config.json file
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var solutionConfig = JsonSerializer.Deserialize<SolutionConfig>(jsonContent);

        if (solutionConfig == null)
        {
            Console.WriteLine("Invalid config.json file.");
            return;
        }

        // Ensure solution paths in config.json have forward slashes for correct matching
        solutionConfig.Solutions = solutionConfig.Solutions.Select(s => s.Replace("\\", "/")).ToList();

        // Check if commit arguments are provided
        if (args.Length == 2)
        {
            string commit1 = args[0];
            string commit2 = args[1];

            Console.WriteLine($"Finding solutions with changes between commits: {commit1} and {commit2} in {solutionConfig.WorkingDirectory}");

            // Use SolutionFinderLib to get the list of solutions with changes between the two commits
            var changedSolutions = SolutionFinder.GetSolutionsWithChanges(commit1, commit2, solutionConfig.WorkingDirectory)
                                                 .Select(s => s.Replace("\\", "/")).ToList();

            if (changedSolutions.Count == 0)
            {
                Console.WriteLine("No solutions contain changed files between the specified commits.");
            }
            else
            {
                Console.WriteLine("Solutions containing changed files:");
                foreach (var solution in changedSolutions)
                {
                    Console.WriteLine("- " + solution);
                }
            }

            // Add solutions from config.json that are not in the changedSolutions
            var forcedSolutions = solutionConfig.Solutions.Except(changedSolutions).ToList();
            solutions.AddRange(changedSolutions);
            solutions.AddRange(forcedSolutions);

            // Output the additional solutions that were added from config.json
            if (forcedSolutions.Count > 0)
            {
                Console.WriteLine("Additional solutions added from config.json:");
                foreach (var solution in forcedSolutions)
                {
                    Console.WriteLine("- " + solution);
                }
            }

            // Ask the user if they want to start the build
            Console.WriteLine("Do you want to start the build? (y/n):");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
            {
                Console.WriteLine("Build process aborted.");
                return;
            }
        }

        // Use existing solutions from config.json if no commit arguments were provided
        if (solutions.Count == 0)
        {
            solutions = solutionConfig.Solutions;
        }
        else
        {
            // Replace solutions in config.json with the found solutions
            solutionConfig.Solutions = solutions;
        }

        // Check if masterBuildOrder.json exists
        if (File.Exists(masterBuildOrderFilePath))
        {
            var masterContent = await File.ReadAllTextAsync(masterBuildOrderFilePath);
            var masterBuildOrder = JsonSerializer.Deserialize<MasterBuildOrder>(masterContent);

            if (masterBuildOrder != null && masterBuildOrder.Solutions != null && masterBuildOrder.Solutions.Count > 0)
            {
                // Ensure paths in masterBuildOrder also have forward slashes
                masterBuildOrder.Solutions = masterBuildOrder.Solutions.Select(s => s.Replace("\\", "/")).ToList();

                // Reorder solutions based on master build order
                solutions = ReorderSolutions(solutionConfig.Solutions, masterBuildOrder.Solutions);
            }
        }

        // Error if no solutions were found
        if (solutions == null || solutions.Count == 0)
        {
            Console.WriteLine("Error: No solutions found in config.json or master build order file.");
            return;
        }

        // Run the commands for each solution
        foreach (var solution in solutions)
        {
            var command = solutionConfig.CommandTemplate.Replace("{solution}", solution);
            Console.WriteLine($"Running command: {command}");

            // Start the build process for the solution
            var buildSuccess = await RunCommandAsync(command);
            if (!buildSuccess)
            {
                Console.WriteLine($"Command failed for solution: {solution}");
                return;
            }
        }

        Console.WriteLine("All commands executed successfully.");
    }

    static List<string> ReorderSolutions(List<string> configSolutions, List<string> masterSolutions)
    {
        var reorderedList = new List<string>();

        // Solutions that are in both config and master, ordered by master
        var matchedSolutions = masterSolutions.Intersect(configSolutions).ToList();
        reorderedList.AddRange(matchedSolutions);

        // Solutions that are in config but not in master
        var unmatchedSolutions = configSolutions.Except(masterSolutions).ToList();
        reorderedList.AddRange(unmatchedSolutions);

        return reorderedList;
    }

    static async Task<bool> RunCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", $"/C {command}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process())
        {
            process.StartInfo = processInfo;

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine("Error: " + args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
    }
}

class SolutionConfig
{
    public string CommandTemplate { get; set; } = "";
    public string WorkingDirectory { get; set; } = ""; // Added field for working directory
    public List<string> Solutions { get; set; } = new List<string>();
}

class MasterBuildOrder
{
    public List<string> Solutions { get; set; } = new List<string>();
}
