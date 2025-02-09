using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Board
    {
        private Tile[,] Tiles { get; }
        private int Size { get; }
        private int BlockSize { get; }
        private Heuristic Heuristic { get; }
        public bool DebugMode { get; }

        public Board(string input, int size, bool debugMode = false)
        {
            if (input.Length != size * size)
                throw new ArgumentException("Input length does not match the expected size.");

            Size = size;
            BlockSize = (int)Math.Sqrt(Size);
            Tiles = new Tile[Size, Size];
            DebugMode = debugMode;
            Heuristic = new Heuristic(Tiles, Size, this); // Pass Board instance to Heuristic

            InitializeBoard(input);

            if (!IsValidInput())
                throw new ArgumentException("The provided Sudoku puzzle contains invalid or conflicting entries.");
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
                        UpdateConstraints(row, col, input[index], false);
                    }
                    index++;
                }
            }

            // Ensure all possible values are correctly identified
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0')
                    {
                        UpdatePossibleValues(row, col);
                    }
                }
            }

            if (DebugMode) LogState("Initial Board Setup:");
        }

        private void UpdatePossibleValues(int row, int col)
        {
            Tiles[row, col].PossibleValuesBitmask = (1L << (Size + 1)) - 2; // All bits set except for the 0th bit

            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value != '0')
                    Tiles[row, col].PossibleValuesBitmask &= ~(1L << (Tiles[row, i].Value - '0'));

                if (Tiles[i, col].Value != '0')
                    Tiles[row, col].PossibleValuesBitmask &= ~(1L << (Tiles[i, col].Value - '0'));
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    if (Tiles[startRow + r, startCol + c].Value != '0')
                        Tiles[row, col].PossibleValuesBitmask &= ~(1L << (Tiles[startRow + r, startCol + c].Value - '0'));
                }
            }

            if (DebugMode)
            {
                Console.WriteLine($"Updated possible values for tile ({row}, {col}): {Convert.ToString(Tiles[row, col].PossibleValuesBitmask, 2).PadLeft(Size + 1, '0')}");
            }
        }

        public bool IsValid()
        {
            for (int i = 0; i < Size; i++)
            {
                if (HasDuplicate(Tiles, i, isRow: true) || HasDuplicate(Tiles, i, isRow: false))
                {
                    if (DebugMode)
                    {
                        Console.WriteLine($"Duplicate found in row or column {i}");
                    }
                    return false;
                }
            }

            for (int blockRow = 0; blockRow < BlockSize; blockRow++)
            {
                for (int blockCol = 0; blockCol < BlockSize; blockCol++)
                {
                    if (HasDuplicateInBlock(Tiles, blockRow * BlockSize, blockCol * BlockSize))
                    {
                        if (DebugMode)
                        {
                            Console.WriteLine($"Duplicate found in block starting at ({blockRow * BlockSize}, {blockCol * BlockSize})");
                        }
                        return false;
                    }
                }
            }

            return true;
        }


        private bool IsValidInput()
        {
            for (int row = 0; row < Size; row++)
            {
                if (HasDuplicate(Tiles, row, isRow: true) || HasDuplicate(Tiles, row, isRow: false))
                    return false;
            }

            for (int blockRow = 0; blockRow < BlockSize; blockRow++)
            {
                for (int blockCol = 0; blockCol < BlockSize; blockCol++)
                {
                    if (HasDuplicateInBlock(Tiles, blockRow * BlockSize, blockCol * BlockSize))
                        return false;
                }
            }

            return true;
        }

        public bool Solve()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            const long timeout = 10000;  // 10 seconds timeout

            bool progress;
            int iteration = 0;
            const int maxIterations = 1000;  // Limit the number of heuristic iterations

            do
            {
                progress = Heuristic.ApplyAll();
                if (DebugMode && progress) LogState($"After Applying Heuristics (Iteration {iteration}):");

                iteration++;
                if (iteration >= maxIterations)
                {
                    Console.WriteLine("Maximum iterations reached. Exiting to prevent infinite loop.");
                    LogState("Final State before Exiting:");
                    return false;
                }

                if (watch.ElapsedMilliseconds > timeout)
                {
                    Console.WriteLine("Solver timed out.");
                    LogState("Final State before Timeout:");
                    return false;
                }
            } while (progress && !IsSolved());

            if (IsSolved())
            {
                LogState("Solved Sudoku:");
                return true;
            }
            else
            {
                LogState("Before Backtracking:");
                var emptyCells = GetEmptyCells();
                bool result = BacktrackSolve(emptyCells, 0);
                LogState(result ? "Solved Sudoku:" : "No solution exists.");
                return result;
            }
        }




        public bool IsSolved()
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0') return false;
                }
            }
            return true;
        }

        public int CountEmptyCells()
        {
            int count = 0;
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0')
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool BacktrackSolve(List<Tuple<int, int>> emptyCells, int depth)
        {
            if (!emptyCells.Any()) return true;

            var cell = emptyCells.First();
            var possibleValues = GetPossibleValues(cell.Item1, cell.Item2);

            foreach (var value in possibleValues)
            {
                Tiles[cell.Item1, cell.Item2].Value = value;
                UpdateConstraints(cell.Item1, cell.Item2, value, false);

                if (IsValid())
                {
                    var remainingCells = emptyCells.Skip(1).ToList();
                    if (BacktrackSolve(remainingCells, depth + 1)) return true;
                }

                Tiles[cell.Item1, cell.Item2].Value = '0';
                UpdateConstraints(cell.Item1, cell.Item2, value, true);
            }

            return false;
        }



        public List<Tuple<int, int>> GetEmptyCells()
        {
            var emptyCells = new List<Tuple<int, int>>();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0')
                        emptyCells.Add(Tuple.Create(row, col));
                }
            }
            return emptyCells.OrderBy(cell => CountSetBits(Tiles[cell.Item1, cell.Item2].PossibleValuesBitmask)).ToList();
        }

        public void UpdateConstraints(int row, int col, char value, bool add)
        {
            long bitMask = 1L << (value - '0');

            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i] != null && i != col && Tiles[row, i].Value == '0')
                    Tiles[row, i].PossibleValuesBitmask = add ? Tiles[row, i].PossibleValuesBitmask | bitMask : Tiles[row, i].PossibleValuesBitmask & ~bitMask;

                if (Tiles[i, col] != null && i != row && Tiles[i, col].Value == '0')
                    Tiles[i, col].PossibleValuesBitmask = add ? Tiles[i, col].PossibleValuesBitmask | bitMask : Tiles[i, col].PossibleValuesBitmask & ~bitMask;
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    int currentRow = startRow + r;
                    int currentCol = startCol + c;
                    if (Tiles[currentRow, currentCol] != null && currentRow != row && currentCol != col && Tiles[currentRow, currentCol].Value == '0')
                        Tiles[currentRow, currentCol].PossibleValuesBitmask = add ? Tiles[currentRow, currentCol].PossibleValuesBitmask | bitMask : Tiles[currentRow, currentCol].PossibleValuesBitmask & ~bitMask;
                }
            }
        }




        public List<char> GetPossibleValues(int row, int col)
        {
            var possibleValues = new List<char>();
            long bitMask = Tiles[row, col].PossibleValuesBitmask;

            for (int i = 1; i <= Size; i++)
            {
                if ((bitMask & (1L << i)) != 0)
                    possibleValues.Add((char)('0' + i));
            }

            return possibleValues;
        }

      
        private bool HasDuplicate(Tile[,] tiles, int index, bool isRow)
        {
            var values = new HashSet<char>();
            for (int i = 0; i < Size; i++)
            {
                char value = isRow ? tiles[index, i].Value : tiles[i, index].Value;
                if (value != '0' && !values.Add(value))
                    return true;
            }
            return false;
        }

        private bool HasDuplicateInBlock(Tile[,] tiles, int startRow, int startCol)
        {
            var values = new HashSet<char>();
            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    char value = tiles[startRow + r, startCol + c].Value;
                    if (value != '0' && !values.Add(value))
                        return true;
                }
            }
            return false;
        }

        public int CountSetBits(long bitMask)
        {
            int count = 0;
            while (bitMask != 0)
            {
                count++;
                bitMask &= (bitMask - 1);
            }
            return count;
        }

        private void LogState(string message)
        {
            Console.WriteLine(message);
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    char value = Tiles[row, col].Value != '0' ? Tiles[row, col].Value : '.';
                    Console.Write(value + " ");
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
                    char value = Tiles[row, col].Value != '0' ? Tiles[row, col].Value : ' ';
                    Console.Write($"{value} ");
                }
                Console.WriteLine();
            }
        }
    }
}