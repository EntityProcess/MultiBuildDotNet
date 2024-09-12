using System;
using SolutionFinderLib;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: SolutionFinderApp <commit1> <commit2> <working-directory>");
            return;
        }

        string commit1 = args[0];
        string commit2 = args[1];
        string workingDirectory = args[2];

        try
        {
            // Use the SolutionFinderLib to get the solutions containing changes
            List<string> solutions = SolutionFinder.GetSolutionsWithChanges(commit1, commit2, workingDirectory);

            if (solutions.Count == 0)
            {
                Console.WriteLine("No solutions contain changed files between the specified commits.");
            }
            else
            {
                Console.WriteLine("Solutions containing changed files:");
                foreach (var solution in solutions)
                {
                    Console.WriteLine(solution);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
