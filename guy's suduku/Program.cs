using System;

namespace guy_s_sudoku
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 16 * 16; i++) { Console.Write("0"); }
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
               DateTime start=DateTime.Now;
                if (board.Solve())
                {
                    Console.WriteLine("Solved Sudoku :");
                    board.PrintBoard();
                }
                else
                {
                    Console.WriteLine("No solution exists.");
                }
                Console.WriteLine("completed in {0} seconds", (DateTime.Now - start).TotalSeconds);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
          
        }
    }
}
