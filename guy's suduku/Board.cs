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

        /* DOES: Initializes a new instance of the Board class with the given input string, size, and debug mode.
           ARGS: input - The input string representing the Sudoku puzzle.
                 size - The size of the Sudoku board.
                 debugMode - Indicates whether debug mode is enabled.
           RETURNS: None.
           RAISES: ArgumentException if the input length does not match the expected size, if the input length is not a perfect fourth power, or if the input contains invalid characters. */
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

        /* DOES: Validates the input string to ensure it contains only valid characters for the given size.
           ARGS: input - The input string representing the Sudoku puzzle.
                 size - The size of the Sudoku board.
           RETURNS: True if the input string is valid, otherwise false.
           RAISES: None. */
        private bool IsValidInputString(string input, int size)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if ((c < '0' || c > '0'+size))
                {
                    return false;
                }
            }
            return true;
        }

        /* DOES: Initializes the Sudoku board with the given input string.
           ARGS: input - The input string representing the Sudoku puzzle.
           RETURNS: None.
           RAISES: None. */
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

        /* DOES: Updates the possible values for a tile based on the current state of the board.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
           RETURNS: None.
           RAISES: None. */
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

        /* DOES: Checks if the current state of the board is valid.
           ARGS: None.
           RETURNS: True if the board is valid, otherwise false.
           RAISES: None. */
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

        /* DOES: Checks if the initial input for the Sudoku puzzle is valid.
           ARGS: None.
           RETURNS: True if the input is valid, otherwise false.
           RAISES: None. */
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

        /* DOES: Solves the Sudoku puzzle using backtracking and heuristics.
           ARGS: None.
           RETURNS: True if the puzzle is solved, otherwise false.
           RAISES: None. */
        public bool Solve()
        {
            var emptyCells = GetEmptyCells();

            // Apply advanced heuristics for larger boards
            if (Size > 9)
            {
                while (Heuristic.ApplyAll())
                {
                    emptyCells = GetEmptyCells(); // Recalculate empty cells after applying heuristics
                }
            }

            return BacktrackSolve(emptyCells, 0);
        }

        /* DOES: Checks if the Sudoku puzzle is solved.
           ARGS: None.
           RETURNS: True if the puzzle is solved, otherwise false.
           RAISES: None. */
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

        /* DOES: Counts the number of empty cells in the Sudoku puzzle.
           ARGS: None.
           RETURNS: The number of empty cells.
           RAISES: None. */
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

        /* DOES: Retrieves a list of empty cells in the Sudoku puzzle.
           ARGS: None.
           RETURNS: A list of tuples representing the row and column indices of empty cells.
           RAISES: None. */
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

        /* DOES: Updates the constraints for a tile based on the value placed or removed.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
                 value - The value to place or remove.
                 add - True to add the value, false to remove it.
           RETURNS: None.
           RAISES: None. */
        public void UpdateConstraints(int row, int col, char value, bool add)
        {
            long bitMask = 1L << (value - '0');
            try
            {
                for (int i = 0; i < Size; i++)
                {
                    if (Tiles[row, i] != null && i != col && Tiles[row, i].Value == '0')
                        Tiles[row, i].PossibleValuesBitmask = add ? Tiles[row, i].PossibleValuesBitmask | bitMask : Tiles[row, i].PossibleValuesBitmask & ~bitMask;

                    if (Tiles[i, col] != null && i != row && Tiles[i, col].Value == '0')
                        Tiles[row, i].PossibleValuesBitmask = add ? Tiles[row, i].PossibleValuesBitmask | bitMask : Tiles[row, i].PossibleValuesBitmask & ~bitMask;
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
            catch (Exception ex)
            { }
        }

        /* DOES: Retrieves the possible values for a tile based on its bitmask.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
           RETURNS: A list of characters representing the possible values.
           RAISES: None. */
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

        /* DOES: Checks if there are duplicate values in a row or column.
           ARGS: tiles - The 2D array of tiles.
                 index - The index of the row or column.
                 isRow - True if checking a row, false if checking a column.
           RETURNS: True if there are duplicate values, otherwise false.
           RAISES: None. */
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

        /* DOES: Checks if there are duplicate values in a block.
           ARGS: tiles - The 2D array of tiles.
                 startRow - The starting row index of the block.
                 startCol - The starting column index of the block.
           RETURNS: True if there are duplicate values, otherwise false.
           RAISES: None. */
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

        /* DOES: Counts the number of set bits in a bitmask.
           ARGS: bitMask - The bitmask to count set bits in.
           RETURNS: The number of set bits in the bitmask.
           RAISES: None. */
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

        /* DOES: Logs the current state of the board with a message.
           ARGS: message - The message to log.
           RETURNS: None.
           RAISES: None. */
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

        /* DOES: Prints the current state of the board.
           ARGS: None.
           RETURNS: None.
           RAISES: None. */
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

        /* DOES: Solves the Sudoku puzzle using backtracking.
           ARGS: emptyCells - A list of tuples representing the row and column indices of empty cells.
                 depth - The current depth of the backtracking algorithm.
           RETURNS: True if the puzzle is solved, otherwise false.
           RAISES: None. */
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

        /* DOES: Performs forward checking to ensure no tile has zero possible values.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
           RETURNS: True if forward checking is successful, otherwise false.
           RAISES: None. */
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
                    if (Tiles[startRow + r, startCol + c].Value == '0' &&
                        Tiles[startRow + r, startCol + c].PossibleValuesBitmask == 0)
                        return false;
                }
            }

            return true;
        }


        /* DOES: Counts the number of constraints for a given value in a tile.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
                 value - The value to count constraints for.
           RETURNS: The number of constraints for the given value.
           RAISES: None. */
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