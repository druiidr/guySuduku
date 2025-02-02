using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Board
    {
        private Tile[,] Tiles;
        private int Size;
        private Heuristic heuristic;
        private bool DebugMode;

        public Board(string input, int size, bool debugMode = false)
        {
            Size = size;
            Tiles = new Tile[Size, Size];
            DebugMode = debugMode;
            InitializeBoard(input);
            if (!IsValidInput())
            {
                throw new ArgumentException("The provided Sudoku puzzle contains invalid or conflicting entries.");
            }
            heuristic = new Heuristic(Tiles, Size);
        }

        private void InitializeBoard(string input)
        {
            int index = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    Tiles[row, col] = new Tile();
                    if (input[index] != '0')
                    {
                        Tiles[row, col].Value = input[index];
                        Tiles[row, col].PossibleValuesBitmask = 0;
                    }
                    index++;
                }
            }

            if (DebugMode)
            {
                LogState("Initial Board Setup:");
            }
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

        public bool Solve()
        {
            bool progress;

            if (DebugMode)
            {
                LogState("Initial Board Setup:");
            }

            do
            {
                progress = false;
                progress |= heuristic.ApplyNakedSingles();
                if (DebugMode && progress)
                {
                    LogState("After Applying Naked Singles:");
                }

                progress |= heuristic.ApplyHiddenSingles();
                if (DebugMode && progress)
                {
                    LogState("After Applying Hidden Singles:");
                }

                progress |= heuristic.ApplyNakedSets();
                if (DebugMode && progress)
                {
                    LogState("After Applying Naked Sets:");
                }
            } while (progress);

            var emptyCells = GetEmptyCells();
            return BacktrackSolve(emptyCells);
        }


        private List<Tuple<int, int>> GetEmptyCells()
        {
            var emptyCells = new List<Tuple<int, int>>();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0')
                    {
                        emptyCells.Add(Tuple.Create(row, col));
                    }
                }
            }
            return emptyCells.OrderBy(cell => CountSetBits(Tiles[cell.Item1, cell.Item2].PossibleValuesBitmask)).ToList();
        }

        private bool BacktrackSolve(List<Tuple<int, int>> emptyCells)
        {
            if (!emptyCells.Any())
            {
                return true;
            }
            var cell = emptyCells.First();
            var possibleValues = GetPossibleValues(cell.Item1, cell.Item2);

            foreach (var value in possibleValues)
            {
                Tiles[cell.Item1, cell.Item2].Value = value;
                long bitMask = 1L << (value - '0');
                UpdateConstraints(cell.Item1, cell.Item2, bitMask, false);

             
                if (IsValid())
                {
                    var remainingCells = emptyCells.Skip(1).ToList();
                    remainingCells = remainingCells.OrderBy(c => CountSetBits(Tiles[c.Item1, c.Item2].PossibleValuesBitmask)).ToList();
                    if (BacktrackSolve(remainingCells))
                    {
                        return true;
                    }
                }
                Tiles[cell.Item1, cell.Item2].Value = '0';
                UpdateConstraints(cell.Item1, cell.Item2, bitMask, true);

            
            }
            return false;
        }

        private void UpdateConstraints(int row, int col, long bitMask, bool add)
        {
            int blockSize = (int)Math.Sqrt(Size);
            int startRow = (row / blockSize) * blockSize;
            int startCol = (col / blockSize) * blockSize;

            // Update row constraints
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0')
                {
                    if (add)
                        Tiles[row, i].PossibleValuesBitmask |= bitMask;
                    else
                        Tiles[row, i].PossibleValuesBitmask &= ~bitMask;
                }
            }

            // Update column constraints
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[i, col].Value == '0')
                {
                    if (add)
                        Tiles[i, col].PossibleValuesBitmask |= bitMask;
                    else
                        Tiles[i, col].PossibleValuesBitmask &= ~bitMask;
                }
            }

            // Update box constraints
            for (int r = 0; r < blockSize; r++)
            {
                for (int c = 0; c < blockSize; c++)
                {
                    if (Tiles[startRow + r, startCol + c].Value == '0')
                    {
                        if (add)
                            Tiles[startRow + r, startCol + c].PossibleValuesBitmask |= bitMask;
                        else
                            Tiles[startRow + r, startCol + c].PossibleValuesBitmask &= ~bitMask;
                    }
                }
            }
        }

        private List<char> GetPossibleValues(int row, int col)
        {
            List<char> possibleValues = new List<char>();
            long bitMask = Tiles[row, col].PossibleValuesBitmask;
            for (int i = 1; i <= Size; i++)
            {
                if ((bitMask & (1L << i)) != 0)
                {
                    possibleValues.Add((char)('0' + i));
                }
            }
            return possibleValues;
        }

        private int CountSetBits(long bitMask)
        {
            int count = 0;
            while (bitMask != 0)
            {
                count++;
                bitMask &= (bitMask - 1);
            }
            return count;
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

        private void LogState(string message)
        {
            Console.WriteLine(message);
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    char value = Tiles[row, col].Value != '0' ? Tiles[row, col].Value : ' ';
                    Console.Write($"{value} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
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
