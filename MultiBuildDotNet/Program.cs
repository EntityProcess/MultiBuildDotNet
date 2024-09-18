using SolutionFinderLib;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Get the current directory and construct paths for config and master build order files
            var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var configFilePath = Path.Combine(executableDirectory, "config.json");
            var masterBuildOrderFilePath = Path.Combine(executableDirectory, "masterBuildOrder.json");

            // Load config.json
            var solutionConfig = await LoadConfigFile(configFilePath);

            // Handle branch comparison or commit comparison
            if (args.Length >= 2 && (args[0] == "-b" || args[0] == "--branch"))
            {
                // Compare the latest commit in the current branch with the latest commit in the specified branch
                var branchToCompare = args[1];
                var currentBranch = GetCurrentBranch();
                var currentBranchLatestCommit = GetLatestCommit(currentBranch);
                var compareBranchLatestCommit = GetLatestCommit(branchToCompare);
                var latestCommonCommit = GetLatestCommonCommit(currentBranch, branchToCompare);

                Console.WriteLine($"Comparing latest commit in current branch ({currentBranch}): {currentBranchLatestCommit} " +
                                  $"with latest commit in branch ({branchToCompare}): {compareBranchLatestCommit}");
                Console.WriteLine($"Latest common commit between {currentBranch} and {branchToCompare}: {latestCommonCommit}");

                // Compare the latest master commit with the latest common commit
                if (IsMasterMoreRecent(compareBranchLatestCommit, latestCommonCommit))
                {
                    Console.WriteLine($"Error: The latest commit in branch ({branchToCompare}) is more recent than the latest common commit ({latestCommonCommit}) in your current branch ({currentBranch}).");
                    Console.WriteLine("Please merge the latest changes from the master branch into your branch before proceeding.");
                    return;
                }

                await FindAndBuildSolutions(currentBranchLatestCommit, compareBranchLatestCommit, configFilePath, masterBuildOrderFilePath);
            }
            else if (args.Length == 2)
            {
                // Compare two specific commits
                var commit1 = args[0];
                var commit2 = args[1];
                Console.WriteLine($"Finding solutions with changes between commits: {commit1} and {commit2}");

                await FindAndBuildSolutions(commit1, commit2, configFilePath, masterBuildOrderFilePath);
            }
            else
            {
                // Show usage instructions
                Console.WriteLine("Usage:");
                Console.WriteLine(" - To compare the latest commits in the current branch and another branch: -b <branch>");
                Console.WriteLine(" - To compare two specific commits: <commit1> <commit2>");
                return;
            }
        }
        catch (FileNotFoundException ex)
        {
            // Graceful handling of missing config or master build order file
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Please ensure that the necessary files (e.g., config.json, masterBuildOrder.json) exist in the application directory.");
            return;
        }
        catch (InvalidOperationException ex)
        {
            // Handle deserialization or other runtime errors gracefully
            Console.WriteLine($"Error: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            // Generic catch for any other unexpected errors
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            return;
        }
    }

    static async Task FindAndBuildSolutions(string commit1, string commit2, string configFilePath, string masterBuildOrderFilePath)
    {
        // Ensure solution paths in config.json have forward slashes for correct matching
        var solutionConfig = await LoadConfigFile(configFilePath);

        if (solutionConfig == null)
        {
            Console.WriteLine("Invalid config.json file.");
            return;
        }

        var changedSolutions = SolutionFinder.GetSolutionsWithChanges(commit1, commit2, solutionConfig.WorkingDirectory)
                                             .Select(s => s.Replace("\\", "/")).ToList();

        if (changedSolutions.Count == 0)
        {
            Console.WriteLine("No solutions contain changed files between the specified commits.");
            return;
        }

        Console.WriteLine("Solutions containing changed files:");
        foreach (var solution in changedSolutions)
        {
            Console.WriteLine("- " + solution);
        }

        // Add solutions from config.json that are not in the changedSolutions
        var forcedSolutions = solutionConfig.Solutions.Except(changedSolutions).ToList();
        changedSolutions.AddRange(forcedSolutions);

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

        var masterBuildOrder = await LoadMasterBuildOrder(masterBuildOrderFilePath);
        await BuildSolutions(changedSolutions, solutionConfig, masterBuildOrder);
    }

    static string GetLatestCommit(string branch)
    {
        // Run git command to get the latest commit hash in the specified branch
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"rev-parse {branch}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var latestCommit = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (string.IsNullOrEmpty(latestCommit))
        {
            throw new InvalidOperationException($"Could not find the latest commit for branch: {branch}");
        }

        return latestCommit;
    }

    static bool IsMasterMoreRecent(string masterCommit, string commonCommit)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"log {commonCommit}..{masterCommit} --oneline",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string logOutput = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        // If git log shows any commits, it means master is more recent
        return !string.IsNullOrEmpty(logOutput);
    }

    static string GetLatestCommonCommit(string branch1, string branch2)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"merge-base {branch1} {branch2}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string commonCommit = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (string.IsNullOrEmpty(commonCommit))
        {
            throw new InvalidOperationException($"Could not find the latest common commit between {branch1} and {branch2}");
        }

        return commonCommit;
    }

    static string GetCurrentBranch()
    {
        // Run git command to get the current branch
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var currentBranch = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (string.IsNullOrEmpty(currentBranch))
        {
            throw new InvalidOperationException("Could not determine the current branch.");
        }

        return currentBranch;
    }

    static async Task<SolutionConfig> LoadConfigFile(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException("config.json file not found at the default location or executable directory.");
        }

        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var solutionConfig = JsonSerializer.Deserialize<SolutionConfig>(jsonContent);

        if (solutionConfig == null)
        {
            throw new InvalidOperationException("Failed to deserialize config.json.");
        }

        return solutionConfig;
    }

    static async Task<MasterBuildOrder> LoadMasterBuildOrder(string masterBuildOrderFilePath)
    {
        if (!File.Exists(masterBuildOrderFilePath))
        {
            return new MasterBuildOrder();
        }

        var masterContent = await File.ReadAllTextAsync(masterBuildOrderFilePath);
        return JsonSerializer.Deserialize<MasterBuildOrder>(masterContent) ?? new MasterBuildOrder();
    }

    static async Task BuildSolutions(List<string> solutions, SolutionConfig solutionConfig, MasterBuildOrder masterBuildOrder)
    {
        if (masterBuildOrder.Solutions != null && masterBuildOrder.Solutions.Count > 0)
        {
            // Ensure paths in masterBuildOrder also have forward slashes
            masterBuildOrder.Solutions = masterBuildOrder.Solutions.Select(s => s.Replace("\\", "/")).ToList();

            // Reorder solutions based on master build order
            solutions = ReorderSolutions(solutions, masterBuildOrder.Solutions);
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

            if (process.ExitCode != 0)
            {
                Console.WriteLine("Build failed for command: " + command);
            }

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
