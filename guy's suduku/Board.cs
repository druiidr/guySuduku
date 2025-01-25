using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
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
            var emptyCells = new List<Tuple<int, int>>();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Tiles[row, col].Value == 0)
                    {
                        UpdatePossibleValues(row, col);
                        emptyCells.Add(Tuple.Create(row, col));
                    }
                }
            }
            return BacktrackSolve(emptyCells);
        }

        private bool BacktrackSolve(List<Tuple<int, int>> emptyCells)
        {
            if (!emptyCells.Any())
            {
                return true;
            }
            var cell = emptyCells.First();
            var possibleValues = Tiles[cell.Item1, cell.Item2].PossibleValues.ToList();
            foreach (var value in possibleValues)
            {
                Tiles[cell.Item1, cell.Item2].Value = value;
                if (IsValid())
                {
                    var remainingCells = emptyCells.Skip(1).ToList();
                    if (BacktrackSolve(remainingCells))
                    {
                        return true;
                    }
                }
                Tiles[cell.Item1, cell.Item2].Value = 0;
            }
            return false;
        }

        private void UpdatePossibleValues(int row, int col)
        {
            var usedValues = new HashSet<int>();
            for (int i = 0; i < 9; i++)
            {
                if (Tiles[row, i].Value != 0) usedValues.Add(Tiles[row, i].Value);
                if (Tiles[i, col].Value != 0) usedValues.Add(Tiles[i, col].Value);
            }
            int startRow = (row / 3) * 3;
            int startCol = (col / 3) * 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3;j++)
                {
                    if (Tiles[startRow + i, startCol + j].Value != 0)
                    {
                        usedValues.Add(Tiles[startRow + i, startCol + j].Value);
                    }
                }
            }
            Tiles[row, col].PossibleValues = new HashSet<int>(Enumerable.Range(1, 9).Except(usedValues));
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
                        if (!rowValues.Add(Tiles[row, col].Value)) return false;
                    }
                    if (Tiles[col, row].Value != 0)
                    {
                        if (!colValues.Add(Tiles[col, row].Value)) return false;
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
                                if (!squareValues.Add(value)) return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool IsValid()
        {
            for (int row = 0; row < 9; row++)
            {
                var rowValues = new HashSet<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (Tiles[row, col].Value != 0 && !rowValues.Add(Tiles[row, col].Value))
                    {
                        return false;
                    }
                }
            }
            for (int col = 0; col < 9; col++)
            {
                var colValues = new HashSet<int>();
                for (int row = 0; row < 9; row++)
                {
                    if (Tiles[row, col].Value != 0 && !colValues.Add(Tiles[row, col].Value))
                    {
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
                            if (value != 0 && !squareValues.Add(value))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
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
