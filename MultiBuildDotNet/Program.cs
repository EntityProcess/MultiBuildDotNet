using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string configFilePath = "config.json"; // Path to your JSON config file
        string masterBuildOrderFilePath = "masterBuildOrder.json"; // Path to master build order config

        if (!File.Exists(configFilePath))
        {
            Console.WriteLine("Config file not found!");
            return;
        }

        if (!File.Exists(masterBuildOrderFilePath))
        {
            Console.WriteLine("Master build order file not found!");
            return;
        }

        // Read the config files
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var masterContent = await File.ReadAllTextAsync(masterBuildOrderFilePath);

        var solutionConfig = JsonSerializer.Deserialize<SolutionConfig>(jsonContent);
        var masterBuildOrder = JsonSerializer.Deserialize<MasterBuildOrder>(masterContent);

        if (solutionConfig == null || solutionConfig.Solutions == null || solutionConfig.Solutions.Count == 0)
        {
            Console.WriteLine("No solutions found in the config file.");
            return;
        }

        if (string.IsNullOrWhiteSpace(solutionConfig.CommandTemplate))
        {
            Console.WriteLine("No command template found in the config file.");
            return;
        }

        if (masterBuildOrder == null || masterBuildOrder.Solutions == null || masterBuildOrder.Solutions.Count == 0)
        {
            Console.WriteLine("No solutions found in the master build order file.");
            return;
        }

        // Reorder solutions based on master build order
        var reorderedSolutions = ReorderSolutions(solutionConfig.Solutions, masterBuildOrder.Solutions);

        foreach (var solution in reorderedSolutions)
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
    public List<string> Solutions { get; set; } = new List<string>();
}

class MasterBuildOrder
{
    public List<string> Solutions { get; set; } = new List<string>();
}
