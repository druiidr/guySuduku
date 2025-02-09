using guy_s_sudoku;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    /*
     * 
     * 
     */
    static void Main(string[] args)
   
    {
        Recieve();
    }

    public static void Recieve()
    {
        bool debugMode = GetDebugMode();
        List<string> puzzles = GetPuzzles(debugMode);

        // The total solve time and solved puzzles count will be calculated in GetPuzzles method
    }

    private static bool GetDebugMode()
    {
        Console.WriteLine("Would you like to enable debug mode? (yes/no)");
        string debugInput = Console.ReadLine().Trim().ToLower();
        return debugInput == "yes";
    }

    private static List<string> GetPuzzles(bool debugMode)
    {
        Console.WriteLine("Choose input method: (1) Direct Input (2) File Input");
        string inputMethod = Console.ReadLine().Trim();

        List<string> puzzles = new List<string>();

        if (inputMethod == "1")
        {
            puzzles = GetPuzzlesFromDirectInput(debugMode);
        }
        else if (inputMethod == "2")
        {
            puzzles = GetPuzzlesFromFileInput(debugMode);
        }
        else
        {
            Console.WriteLine("Invalid input method.");
            return GetPuzzles(debugMode);
        }

        return puzzles;
    }

    private static List<string> GetPuzzlesFromDirectInput(bool debugMode)
    {
        List<string> puzzles = new List<string>();
        double totalSolveTime = 0;
        int solvedPuzzles = 0;

        while (true)
        {
            Console.WriteLine("Enter a Sudoku puzzle to solve (single continuous string, must be a square, 0 indicates empty space), or 'quit' to quit:");
            string input = Console.ReadLine().Trim();
            if (input.ToLower() == "quit")
            {
                break;
            }

            puzzles.Add(input);
            int size = (int)Math.Sqrt(input.Length);

            try
            {
                Board board = new Board(input, size, debugMode);
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

        return puzzles;
    }

    private static List<string> GetPuzzlesFromFileInput(bool debugMode)
    {
        List<string> puzzles = new List<string>();

        Console.WriteLine("Enter the file path:");
        string filePath = Console.ReadLine().Trim();
        try
        {
            puzzles.AddRange(File.ReadAllLines(filePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return GetPuzzlesFromFileInput(debugMode);
        }

        SolvePuzzles(puzzles, debugMode);

        return puzzles;
    }

    private static void SolvePuzzles(List<string> puzzles, bool debugMode)
    {
        double totalSolveTime = 0;
        int solvedPuzzles = 0;

        var lockObject = new object();

        Parallel.ForEach(puzzles, puzzle =>
        {
            int size = (int)Math.Sqrt(puzzle.Length);

            try
            {
                Board board = new Board(puzzle, size, debugMode);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (board.Solve())
                {
                    lock (lockObject)
                    {
                        Console.WriteLine("Solved Sudoku:");
                        board.PrintBoard();
                        solvedPuzzles++;
                    }
                }
                else
                {
                    lock (lockObject)
                    {
                        Console.WriteLine("No solution exists.");
                    }
                }
                stopwatch.Stop();
                double solveTime = stopwatch.Elapsed.TotalSeconds;
                lock (lockObject)
                {
                    totalSolveTime += solveTime;
                    Console.WriteLine($"Completed in {solveTime} seconds");
                }
            }
            catch (ArgumentException ex)
            {
                lock (lockObject)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        });

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
