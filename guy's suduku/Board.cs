using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace guy_s_suduku
{
    internal class Board
    {
        private Tile[,] Tiles;
        public Board(string input)
        {
            Tiles = new Tile[9, 9];
            int index = 0;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Tiles[row, col] = new Tile();

                    if (char.IsDigit(input[index]) && input[index] != '0')
                    {
                        Tiles[row, col].Value = input[index] - '0';
                        Tiles[row, col].PossibleValues.Clear();
                    }

                    index++;
                }
            }

            if (!IsValidInput())
            {
                throw new ArgumentException("The provided Sudoku puzzle contains invalid or conflicting entries.");
            }
        }

        public bool Solve()
        {
            bool progress;

            do
            {
                progress = false;

                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        if (Tiles[row, col].Value == 0)
                        {
                            UpdatePossibleValues(row, col);

                            if (Tiles[row, col].PossibleValues.Count == 0)
                            {
                                throw new InvalidOperationException("The Sudoku puzzle cannot be solved.");
                            }

                            if (Tiles[row, col].PossibleValues.Count == 1)
                            {
                                Tiles[row, col].Value = Tiles[row, col].PossibleValues.First();
                                Tiles[row, col].PossibleValues.Clear();
                                progress = true;
                            }
                        }
                    }
                }
            } while (progress);

            if (IsSolved())
            {
                return true;
            }

            // Backtracking for unresolved cells
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Tiles[row, col].Value == 0)
                    {
                        foreach (int value in Tiles[row, col].PossibleValues)
                        {
                            Board copy = CreateCopy();
                            copy.Tiles[row, col].Value = value;
                            copy.Tiles[row, col].PossibleValues.Clear();

                            try
                            {
                                if (copy.Solve())
                                {
                                    Array.Copy(copy.Tiles, Tiles, Tiles.Length);
                                    return true;
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // Ignore and try the next value
                            }
                        }
                        return false; // No solution found
                    }
                }
            }

            return false;
        }

        private void UpdatePossibleValues(int row, int col)
        {
            var usedValues = new HashSet<int>();

            // Check row and column
            for (int i = 0; i < 9; i++)
            {
                if (Tiles[row, i].Value != 0) usedValues.Add(Tiles[row, i].Value);
                if (Tiles[i, col].Value != 0) usedValues.Add(Tiles[i, col].Value);
            }

            // Check subgrid
            int startRow = (row / 3) * 3;
            int startCol = (col / 3) * 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Tiles[startRow + i, startCol + j].Value != 0)
                    {
                        usedValues.Add(Tiles[startRow + i, startCol + j].Value);
                    }
                }
            }

            Tiles[row, col].PossibleValues.ExceptWith(usedValues);
        }

        private bool IsSolved()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Tiles[row, col].Value == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsValidInput()
        {
            for (int row = 0; row < 9; row++)
            {
                var rowValues = new HashSet<int>();
                var colValues = new HashSet<int>();

                for (int col = 0; col < 9; col++)
                {
                    if (Tiles[row, col].Value != 0)
                    {
                        if (!rowValues.Add(Tiles[row, col].Value))
                            return false;
                    }

                    if (Tiles[col, row].Value != 0)
                    {
                        if (!colValues.Add(Tiles[col, row].Value))
                            return false;
                    }
                }
            }

            for (int startRow = 0; startRow < 9; startRow += 3)
            {
                for (int startCol = 0; startCol < 9; startCol += 3)
                {
                    var squareValues = new HashSet<int>();

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int value = Tiles[startRow + i, startCol + j].Value;

                            if (value != 0)
                            {
                                if (!squareValues.Add(value))
                                    return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private Board CreateCopy()
        {
            var copy = new Board(new string('0', 81));

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    copy.Tiles[row, col].Value = Tiles[row, col].Value;
                    copy.Tiles[row, col].PossibleValues = new HashSet<int>(Tiles[row, col].PossibleValues);
                }
            }

            return copy;
        }

        public void PrintBoard()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Console.Write(Tiles[row, col].Value + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
