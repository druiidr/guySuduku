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

            double fourthRoot = Math.Sqrt(Math.Sqrt(input.Length));
            if (fourthRoot % 1 != 0)
                throw new ArgumentException("Input length must be a perfect fourth power to form a valid Sudoku grid.");

            if (!IsValidInputString(input, size))
                throw new ArgumentException("Input contains invalid characters.");

            Size = size;
            BlockSize = (int)Math.Sqrt(Size);
            Tiles = new Tile[Size, Size];
            DebugMode = debugMode;
            Heuristic = new Heuristic(Tiles, Size, this); // Pass Board instance to Heuristic

            InitializeBoard(input);

            if (!IsValidInput())
                throw new ArgumentException("The provided Sudoku puzzle contains invalid or conflicting entries.");
        }

        private bool IsValidInputString(string input, int size)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c != '0' && (c < '1' || c > '9') && (c < 'A' || c > (char)('A' + size - 10)))
                {
                    return false;
                }
            }
            return true;
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
                Console.WriteLine($"Updated possible values for tile ({row},{col}): {Convert.ToString(Tiles[row, col].PossibleValuesBitmask, 2).PadLeft(Size + 1, '0')}");
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
            var emptyCells = GetEmptyCells();
            return BacktrackSolve(emptyCells, 0);
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
                    if (currentRow < Size && currentCol < Size && Tiles[currentRow, currentCol] != null && currentRow != row && currentCol != col && Tiles[currentRow, currentCol].Value == '0')
                    {
                        Tiles[currentRow, currentCol].PossibleValuesBitmask = add ? Tiles[currentRow, currentCol].PossibleValuesBitmask | bitMask : Tiles[currentRow, currentCol].PossibleValuesBitmask & ~bitMask;
                    }
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
        private bool BacktrackSolve(List<Tuple<int, int>> emptyCells, int depth)
        {
            if (depth >= emptyCells.Count)
            {
                return IsSolved();
            }

            var (row, col) = emptyCells[depth];
            var possibleValues = GetPossibleValues(row, col);

            // Sort possible values by the least constraining value heuristic
            possibleValues = possibleValues.OrderBy(value => CountConstraints(row, col, value)).ToList();

            foreach (var value in possibleValues)
            {
                if (Heuristic.IsValidMove(row, col, value))
                {
                    Tiles[row, col].Value = value;
                    UpdateConstraints(row, col, value, false);

                    if (ForwardCheck(row, col))
                    {
                        if (BacktrackSolve(emptyCells, depth + 1))
                        {
                            return true;
                        }
                    }

                    // Undo move
                    Tiles[row, col].Value = '0';
                    UpdateConstraints(row, col, value, true);
                }
            }

            return false;
        }

        private bool ForwardCheck(int row, int col)
        {
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0' && Tiles[row, i].PossibleValuesBitmask == 0)
                    return false;
                if (Tiles[i, col].Value == '0' && Tiles[i, col].PossibleValuesBitmask == 0)
                    return false;
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    if (Tiles[startRow + r, startCol + c].Value == '0' && Tiles[startRow + r, startCol + c].PossibleValuesBitmask == 0)
                        return false;
                }
            }

            return true;
        }

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