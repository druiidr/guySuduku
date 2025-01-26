using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Board
    {
        private Tile[,] Tiles;
        private int Size;

        public Board(string input, int size)
        {
            Size = size;
            Tiles = new Tile[Size, Size];
            int index = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    Tiles[row, col] = new Tile();
                    if (char.IsDigit(input[index]) && input[index] != '0')
                    {
                        Tiles[row, col].Value = input[index];
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
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0')
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
                Tiles[cell.Item1, cell.Item2].Value = '0';
            }
            return false;
        }

        private void UpdatePossibleValues(int row, int col)
        {
            var usedValues = new HashSet<int>();
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value != '0') usedValues.Add(Tiles[row, i].Value);
                if (Tiles[i, col].Value != '0') usedValues.Add(Tiles[i, col].Value);
            }
            int blockSize = (int)Math.Sqrt(Size);
            int startRow = (row / blockSize) * blockSize;
            int startCol = (col / blockSize) * blockSize;
            for (int i = 0; i < blockSize; i++)
            {
                for (int j = 0; j < blockSize; j++)
                {
                    if (Tiles[startRow + i, startCol + j].Value != '0')
                    {
                        usedValues.Add(Tiles[startRow + i, startCol + j].Value);
                    }
                }
            }
            Tiles[row, col].PossibleValues = new HashSet<int>(Enumerable.Range(49, Size).Except(usedValues));
        }

        private bool IsValidInput()
        {
            for (int row = 0; row < Size; row++)
            {
                var rowValues = new HashSet<int>();
                var colValues = new HashSet<int>();
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value != '0')
                    {
                        if (!rowValues.Add(Tiles[row, col].Value)) return false;
                    }
                    if (Tiles[col, row].Value != '0')
                    {
                        if (!colValues.Add(Tiles[col, row].Value)) return false;
                    }
                }
            }
            int blockSize = (int)Math.Sqrt(Size);
            for (int startRow = 0; startRow < Size; startRow += blockSize)
            {
                for (int startCol = 0; startCol < Size; startCol += blockSize)
                {
                    var squareValues = new HashSet<int>();
                    for (int i = 0; i < blockSize; i++)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            int value = Tiles[startRow + i, startCol + j].Value;
                            if (value != '0')
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
            for (int row = 0; row < Size; row++)
            {
                var rowValues = new HashSet<int>();
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value != '0' && !rowValues.Add(Tiles[row, col].Value))
                    {
                        return false;
                    }
                }
            }
            for (int col = 0; col < Size; col++)
            {
                var colValues = new HashSet<int>();
                for (int row = 0; row < Size; row++)
                {
                    if (Tiles[row, col].Value != '0' && !colValues.Add(Tiles[row, col].Value))
                    {
                        return false;
                    }
                }
            }
            int blockSize = (int)Math.Sqrt(Size);
            for (int startRow = 0; startRow < Size; startRow += blockSize)
            {
                for (int startCol = 0; startCol < Size; startCol += blockSize)
                {
                    var squareValues = new HashSet<int>();
                    for (int i = 0; i < blockSize; i++)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            int value = Tiles[startRow + i, startCol + j].Value;
                            if (value != '0' && !squareValues.Add(value))
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
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    char value = Tiles[row, col].Value != '0' ? (char)Tiles[row, col].Value : ' ';
                    Console.Write($"{value} ");
                }
                Console.WriteLine();
            }
        }
    }
}
