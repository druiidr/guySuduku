using guy_s_sudoku;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Would you like to enable debug mode? (yes/no)");
        string debugInput = Console.ReadLine().Trim().ToLower();
        bool debugMode = debugInput == "yes";

        Console.WriteLine("Choose input method: (1) Direct Input (2) File Input");
        string inputMethod = Console.ReadLine().Trim();

        List<string> puzzles = new List<string>();

        if (inputMethod == "1")
        {
            while (true)
            {
                Console.WriteLine("Enter the Sudoku puzzle as a single string (length must be a perfect square, use 0 for empty cells) or type 'quit' to finish:");
                string input = Console.ReadLine().Trim();
                if (input.ToLower() == "quit")
                {
                    break;
                }
                puzzles.Add(input);
            }
        }
        else if (inputMethod == "2")
        {
            Console.WriteLine("Enter the file path:");
            string filePath = Console.ReadLine().Trim();
            try
            {
                puzzles.AddRange(File.ReadAllLines(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return;
            }
        }
        else
        {
            Console.WriteLine("Invalid input method.");
            return;
        }

        double totalSolveTime = 0;
        int solvedPuzzles = 0;

        foreach (var puzzle in puzzles)
        {
            int size = (int)Math.Sqrt(puzzle.Length);

            try
            {
                Board board = new Board(puzzle, size, debugMode);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (board.Solve())
                {
                    Console.WriteLine("Solved Sudoku:");
                    board.PrintBoard();
                    solvedPuzzles++;
                }
                else
                {
                    Console.WriteLine("No solution exists.");
                }
                stopwatch.Stop();
                double solveTime = stopwatch.Elapsed.TotalSeconds;
                totalSolveTime += solveTime;
                Console.WriteLine($"Completed in {solveTime} seconds");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        if (solvedPuzzles > 0)
        {
            double averageSolveTime = totalSolveTime / solvedPuzzles;
            Console.WriteLine($"Total solve time for all puzzles: {totalSolveTime} seconds");
            Console.WriteLine($"Average solve time per puzzle: {averageSolveTime} seconds");
        }
        else
        {
            Console.WriteLine("No puzzles were solved.");
        }
    }
}
