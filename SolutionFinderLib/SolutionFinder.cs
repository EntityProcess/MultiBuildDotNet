using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SolutionFinderLib
{
    public class SolutionFinder
    {
        /// <summary>
        /// Gets a list of solution file paths that contain files changed between two commits within the specified working directory.
        /// </summary>
        /// <param name="commit1">First Git commit hash.</param>
        /// <param name="commit2">Second Git commit hash.</param>
        /// <param name="workingDirectory">The directory where the solutions and Git repository are located.</param>
        /// <returns>A list of full paths to .sln files that contain the changed files.</returns>
        public static List<string> GetSolutionsWithChanges(string commit1, string commit2, string workingDirectory)
        {
            // Run git diff --name-only to get changed files
            List<string> changedFiles = GetChangedFiles(commit1, commit2, workingDirectory);

            if (changedFiles == null || changedFiles.Count == 0)
            {
                return new List<string>(); // No files changed
            }

            // Find solutions containing changes
            HashSet<string> solutionsContainingChanges = new HashSet<string>();

            foreach (var changedFile in changedFiles)
            {
                string changedFileFullPath = Path.GetFullPath(Path.Combine(workingDirectory, changedFile));
                string currentDirectory = Path.GetDirectoryName(changedFileFullPath) ?? string.Empty;

                // Traverse up the directory tree to find the first .sln file
                while (!string.IsNullOrEmpty(currentDirectory))
                {
                    // Check if there's a .sln file in the current directory
                    var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln", SearchOption.TopDirectoryOnly);

                    if (solutionFiles.Length > 0)
                    {
                        // Add the first solution found and stop searching for this changed file
                        solutionsContainingChanges.Add(solutionFiles.First());
                        break;
                    }

                    // Move up to the parent directory
                    currentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? string.Empty;
                }
            }

            return solutionsContainingChanges.ToList();
        }

        /// <summary>
        /// Runs git diff --name-only to get a list of files changed between two commits within the specified directory.
        /// </summary>
        /// <param name="commit1">First Git commit hash.</param>
        /// <param name="commit2">Second Git commit hash.</param>
        /// <param name="workingDirectory">The directory where the Git repository is located.</param>
        /// <returns>A list of file paths that have changed.</returns>
        private static List<string> GetChangedFiles(string commit1, string commit2, string workingDirectory)
        {
            try
            {
                // Create a new process to run 'git diff --name-only <commit1> <commit2>'
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"diff --name-only {commit1} {commit2}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory // Set the working directory for the git command
                    }
                };

                process.Start();
                List<string> changedFiles = new List<string>();

                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine() ?? string.Empty;
                    if (!string.IsNullOrEmpty(line))
                    {
                        changedFiles.Add(line);
                    }
                }

                process.WaitForExit();
                return changedFiles;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while getting changed files.", ex);
            }
        }
    }
}
