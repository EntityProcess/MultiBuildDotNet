using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string configFilePath = "config.json"; // Path to your JSON config file

        if (!File.Exists(configFilePath))
        {
            Console.WriteLine("Config file not found!");
            return;
        }

        // Read the config file
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var solutionConfig = JsonSerializer.Deserialize<SolutionConfig>(jsonContent);

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

        foreach (var solution in solutionConfig.Solutions)
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
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            Console.WriteLine(output);

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine("Error:");
                Console.WriteLine(error);
            }

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
