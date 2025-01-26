using System;
using System.IO;
using System.Linq;

namespace guy_s_sudoku
{
    class Program
    {
        public static void Main(string[] args)
        {
            DateTime start, end;
            string input;
            int size = 0;
            Console.WriteLine("Would you like to enter the Sudoku puzzle directly or provide a file path? (Enter 'direct' or 'file')");
            string inputMethod = Console.ReadLine().ToLower();

            try
            {
                if (inputMethod == "file")
                {
                    Console.WriteLine("Enter the path to the Sudoku puzzle file:");
                    string filePath = Console.ReadLine();
                    input = File.ReadAllText(filePath).Trim();
                }
                else if (inputMethod == "direct")
                {
                    Console.WriteLine("Enter the Sudoku puzzle as a single string (length must be a perfect square, use 0 for empty cells):");
                    input = Console.ReadLine().Trim();
                }
                else
                {
                    Console.WriteLine("Invalid input method. Please enter either 'direct' or 'file'.");
                    return;
                }

                size = (int)Math.Sqrt(input.Length);
                Constants.SQUARE_PARAMS = size;
                if (input.Length != size * size || !input.All(c => (c == '0' || (c >= '1' && c <= (char)('0' + size)))))
                {
                    Console.WriteLine("Invalid input. Ensure it is a valid Sudoku puzzle with appropriate length and characters.");
                    return;
                }

                Board board = new Board(input, size);
                start = DateTime.Now;
                if (board.Solve())
                {
                    Console.WriteLine("Solved Sudoku:");
                    board.PrintBoard();
                }
                else
                {
                    Console.WriteLine("No solution exists.");
                }
                end = DateTime.Now;
                Console.WriteLine("It took me {0} seconds to determine my answer", (end - start).TotalSeconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
