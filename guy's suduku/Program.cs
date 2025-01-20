using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_suduku
{
    class program
    {
        public static void Main(string[] args)
        {
            DateTime start, end;
            string input;
            Console.WriteLine("Enter the Sudoku puzzle as a single string (81 characters, use 0 for empty cells):");
            try
            {
                input = Console.ReadLine();

                if (input.Length != 81 || !input.All(c => char.IsDigit(c)))
                {
                    Console.WriteLine("Invalid input. Ensure it is exactly 81 digits long and contains only numbers.");
                    return;
                }

                Board board = new Board(input);
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
                end= DateTime.Now;
                Console.WriteLine("it took me {0} seconds to determine my answer", (double)((end-start).Milliseconds)/1000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
