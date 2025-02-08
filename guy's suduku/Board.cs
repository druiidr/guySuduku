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
        private bool DebugMode { get; }

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

            if (DebugMode) LogState("Initial Board Setup:");
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
            bool progress;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long timeout = 1000;  // 10 seconds timeout for demonstration

            if (DebugMode)
            {
                LogState("Initial Board Setup:");
            }

            do
            {
                progress = false;
                progress |= Heuristic.ApplyNakedSingles();
                if (DebugMode && progress)
                {
                    LogState("After Applying Naked Singles:");
                }

                progress |= Heuristic.ApplyHiddenSingles();
                if (DebugMode && progress)
                {
                    LogState("After Applying Hidden Singles:");
                }

                // Apply complex heuristics only for larger puzzles
                if (Size >= 16)
                {
                    progress |= Heuristic.ApplyNakedSets();
                    if (DebugMode && progress)
                    {
                        LogState("After Applying Naked Sets:");
                    }

                    progress |= Heuristic.ApplySimplePairs();
                    if (DebugMode && progress)
                    {
                        LogState("After Applying Simple Pairs:");
                    }
                }

                if (watch.ElapsedMilliseconds > timeout)
                {
                    Console.WriteLine("Solver timed out.");
                    return false;
                }

            } while (progress);

            LogState("Before Backtracking:");
            var emptyCells = GetEmptyCells();
            bool result = BacktrackSolve(emptyCells,0);
            LogState(result ? "Solved Sudoku:" : "No solution exists.");
            return result;
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

        public bool BacktrackSolve(List<Tuple<int, int>> emptyCells, int depth)
        {
            const int maxDepth = 1000; // Adjust as needed
            if (depth > maxDepth)
            {
                Console.WriteLine("Maximum recursion depth reached. Exiting to prevent infinite loop.");
                return false;
            }

            if (!emptyCells.Any()) return true;

            var cell = emptyCells.First();
            var possibleValues = GetPossibleValues(cell.Item1, cell.Item2);

            foreach (var value in possibleValues)
            {
                Tiles[cell.Item1, cell.Item2].Value = value;
                UpdateConstraints(cell.Item1, cell.Item2, value, false);

                

                if (IsValid() && BacktrackSolve(emptyCells.Skip(1).ToList(), depth + 1))
                    return true;

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


        private void UpdateConstraints(int row, int col, char value, bool add)
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


        private List<char> GetPossibleValues(int row, int col)
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

        private bool IsValid()
        {
            for (int i = 0; i < Size; i++)
            {
                if (HasDuplicate(Tiles, i, isRow: true) || HasDuplicate(Tiles, i, isRow: false))
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
                    char value = Tiles[row, col].Value != '0' ? Tiles[row, col].Value : ' ';
                    Console.Write($"{value} ");
                }
                Console.WriteLine();
            }
        }
    }
}
