using guy_s_sudoku;

class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 256; i++)
        {
            Console.Write('0');
        }
        Console.WriteLine();
        Console.WriteLine("Would you like to enable debug mode? (yes/no)");
        string debugInput = Console.ReadLine().Trim().ToLower();
        bool debugMode = debugInput == "yes";

        Console.WriteLine("Enter the Sudoku puzzle as a single string (length must be a perfect square, use 0 for empty cells):");
        string input = Console.ReadLine().Trim();

        int size = (int)Math.Sqrt(input.Length);

        try
        {
            Board board = new Board(input, size, debugMode);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (board.Solve())
            {
                Console.WriteLine("Solved Sudoku:");
                board.PrintBoard();
            }
            else
            {
                Console.WriteLine("No solution exists.");
            }
            stopwatch.Stop();
            Console.WriteLine($"Completed in {stopwatch.Elapsed.TotalSeconds} seconds");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}