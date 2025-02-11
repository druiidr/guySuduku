using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Board
    {
        private Tile[,] Tiles { get; }
        public int Size { get; }
        private int BlockSize { get; }
        private Heuristic Heuristic { get; }
        public bool DebugMode { get; }
        private Stopwatch profiler = new Stopwatch();
        private int maxBacktrackDepth = 0;
        private Dictionary<int, int> backtrackDepthCounts = new Dictionary<int, int>();

        public Board(string input, int size, bool debugMode = false)
        {
            Size = size;
            BlockSize = (int)Math.Sqrt(Size);
            Tiles = new Tile[size, size];
            DebugMode = debugMode;
            Heuristic = new Heuristic(Tiles, Size, this);
            InitializeBoard(input);
        }
        /// <summary>
        /// IsValidInput method to check if the input is valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValidInput()
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
        /// <summary>
        /// InitializeBoard method to initialize the board.
        /// </summary>
        /// <param name="input"></param>
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
                    }
                    index++;
                }
            }

            PropagateInitialConstraints(); // Ensure constraints are uniformly propagated from all initial values

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

        /// <summary>
        /// Propagate constraints from initial values.
        /// </summary>
        private void PropagateInitialConstraints()
        {
            bool changed;
            do
            {
                changed = false;
                for (int row = 0; row < Size; row++)
                {
                    for (int col = 0; col < Size; col++)
                    {
                        if (Tiles[row, col].Value == '0' && CountSetBits(Tiles[row, col].PossibleValuesBitmask) == 1)
                        {
                            char forcedValue = GetPossibleValues(row, col).First();
                            Tiles[row, col].Value = forcedValue;
                            UpdateConstraints(row, col, forcedValue, false);
                            changed = true;
                        }
                    }
                }
            } while (changed);
        }


        /// <summary>
        /// UpdatePossibleValues method to update the possible values for a given position.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
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
        }
        /// <summary>
        /// Solve method to solve the puzzle.
        /// </summary>
        /// <returns></returns>
        public bool Solve()
        {
            profiler.Restart();
            maxBacktrackDepth = 0;
            backtrackDepthCounts.Clear();
            PropagateInitialConstraints();
            var emptyCells = GetEmptyCells();
            emptyCells = emptyCells.OrderBy(cell => CountSetBits(Tiles[cell.Item1, cell.Item2].PossibleValuesBitmask))
                                   .ThenBy(cell => CountConstraints(cell.Item1, cell.Item2, Tiles[cell.Item1, cell.Item2].Value))
                                   .ToList();

            Console.WriteLine($"Starting Solve - Empty Cells: {emptyCells.Count}");
            bool result = BacktrackSolve(emptyCells, 0);
            profiler.Stop();

            Console.WriteLine($"Solve Complete: {result} in {profiler.ElapsedMilliseconds}ms");
            Console.WriteLine($"Max Backtracking Depth: {maxBacktrackDepth}");
            return result;
        }
        /// <summary>
        /// IsSolved method to check if the puzzle is solved.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// CountEmptyCells method to count the empty cells.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// GetEmptyCells method to get the empty cells.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        ///     UpdateConstraints method to update the constraints for a given value at a given position.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        /// <param name="add"></param>
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
                    if (currentRow < Size && currentCol < Size && Tiles[currentRow, currentCol] != null && currentRow != row && currentCol != col && Tiles[currentRow, currentCol].Value == '0')
                    {
                        Tiles[currentRow, currentCol].PossibleValuesBitmask = add ? Tiles[currentRow, currentCol].PossibleValuesBitmask | bitMask : Tiles[currentRow, currentCol].PossibleValuesBitmask & ~bitMask;
                    }
                }
            }
        }
        /// <summary>
        /// GetPossibleValues method to get the possible values for a given position.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
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
        /// <summary>
        /// HasDuplicate method to check if there is a duplicate in a row or column.
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="index"></param>
        /// <param name="isRow"></param>
        /// <returns></returns>
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
        /// <summary>
        ///    HasDuplicateInBlock method to check if there is a duplicate in a block.
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        /// <returns></returns>
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
        /// <summary>
        ///     CountSetBits method to count the set bits in a given bitmask.
        /// </summary>
        /// <param name="bitMask"></param>
        /// <returns></returns>
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
        /// <summary>
        /// LogState method to log the state of the board.
        /// </summary>
        /// <param name="message"></param>
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
        /// <summary>
        /// PrintBoard method to print the board.
        /// </summary>
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
        /// <summary>
        /// BacktrackSolve method to solve the puzzle using backtracking.
        /// </summary>
        /// <param name="emptyCells"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private bool BacktrackSolve(List<Tuple<int, int>> emptyCells, int depth)
        {
            if (depth >= emptyCells.Count)
                return true;

            if (depth > maxBacktrackDepth)
                maxBacktrackDepth = depth;

            var (row, col) = emptyCells[depth];
            var possibleValues = GetPossibleValues(row, col).OrderBy(value => CountConstraints(row, col, value)).ToList();

            foreach (var value in possibleValues)
            {
                if (Heuristic.IsValidMove(row, col, value))
                {
                    Tiles[row, col].Value = value;
                    UpdateConstraints(row, col, value, false);
                    if (ForwardCheck(row, col) && !HasEmptyDomain())
                    {
                        if (BacktrackSolve(emptyCells, depth + 1))
                            return true;
                    }
                    Tiles[row, col].Value = '0';
                    UpdateConstraints(row, col, value, true);
                }
            }
            return false;
        }

        /// <summary>
        /// ForwardCheck method to check if the move is valid.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private bool ForwardCheck(int row, int col)
        {
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0' && CountSetBits(Tiles[row, i].PossibleValuesBitmask) == 0)
                    return false;
                if (Tiles[i, col].Value == '0' && CountSetBits(Tiles[i, col].PossibleValuesBitmask) == 0)
                    return false;
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;
            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    if (Tiles[startRow + r, startCol + c].Value == '0' && CountSetBits(Tiles[startRow + r, startCol + c].PossibleValuesBitmask) == 0)
                        return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool HasEmptyDomain()
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0' && CountSetBits(Tiles[row, col].PossibleValuesBitmask) == 0)
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// CountConstraints method to count the constraints for a given value at a given position.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private int CountConstraints(int row, int col, char value)
        {
            int constraints = 0;
            long bitMask = 1L << (value - '0');

            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0' && (Tiles[row, i].PossibleValuesBitmask & bitMask) != 0)
                    constraints++;
                if (Tiles[i, col].Value == '0' && (Tiles[i, col].PossibleValuesBitmask & bitMask) != 0)
                    constraints++;
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    if (Tiles[startRow + r, startCol + c].Value == '0' && (Tiles[startRow + r, startCol + c].PossibleValuesBitmask & bitMask) != 0)
                        constraints++;
                }
            }

            return constraints;
        }
    }
}