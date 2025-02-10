using System;
using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Heuristic
    {
        private readonly Tile[,] Tiles;
        private readonly int Size;
        private readonly int BlockSize;
        private readonly Board board;


        /* DOES: Initializes the Heuristic class with the given tiles, size, and board reference.
             ARGS: tiles - The 2D array of tiles representing the board.
                   size - The size of the board.
                   board - The reference to the Board class.
             RETURNS: None.
             RAISES: None. */
        public Heuristic(Tile[,] tiles, int size, Board board)
        {
            Tiles = tiles;
            Size = size;
            BlockSize = (int)Math.Sqrt(size);
            this.board = board; // Initialize Board reference
        }

       /* DOES: Applies all heuristic strategies iteratively until no progress is made or the board is solved.
         ARGS: None.
         RETURNS: True if progress is made, otherwise false.
         RAISES: None.*/
        public bool ApplyAll()
        {
            bool progress;
            int iterations = 0;
            const int maxIterations = 100000;

            do
            {
                progress = ApplyHeuristics();
                iterations++;

                if (board.DebugMode)
                {
                    Console.WriteLine($"Iteration {iterations}, Progress: {progress}");
                    board.PrintBoard(); // Print board state after each iteration
                }

                if (iterations > maxIterations)
                {
                    Console.WriteLine("Maximum iterations reached. Exiting to prevent infinite loop.");
                    break;
                }

            } while (progress && !board.IsSolved());

            return progress;
        }

        /* DOES: Applies heuristic strategies in a priority order (Naked Singles, Hidden Singles, etc.)
        ARGS: None.
        RETURNS: True if progress is made, otherwise false.
        RAISES: None.*/
        private bool ApplyHeuristics()
        {
            bool progress = false;

            // Apply Naked Singles first as they are the simplest and most effective
            progress = ApplyNakedSingles();

            if (board.DebugMode)
            {
                Console.WriteLine("After applying Naked Singles:");
                board.PrintBoard();
            }

            // Apply Hidden Singles if Naked Singles did not make much progress
            if (!progress || board.CountEmptyCells() < GetAdaptiveThreshold())
            {
                progress |= ApplyHiddenSingles();

                if (board.DebugMode)
                {
                    Console.WriteLine("After applying Hidden Singles:");
                    board.PrintBoard();
                }
            }

            // Apply Naked Sets and Simple Pairs if the board is still not solved
            if (!progress || board.CountEmptyCells() < GetAdaptiveThreshold())
            {
                progress |= ApplyNakedSets() || ApplySimplePairs();

                if (board.DebugMode)
                {
                    Console.WriteLine("After applying Naked Sets and Simple Pairs:");
                    board.PrintBoard();
                }
            }

            return progress;
        }
        /* DOES: Determines the threshold for applying deeper heuristics based on board size.
           ARGS: None.
           RETURNS: An integer threshold value.
           RAISES: None..*/
        private int GetAdaptiveThreshold()
        {
            // Set adaptive threshold based on board size
            if (Size <= 4) return 5; // Smaller boards
            if (Size <= 9) return 20; // Medium boards
            return 30; // Larger boards
        }

        /* DOES: Applies the Naked Singles heuristic by setting values when only one possible value remains.
         ARGS: None.
         RETURNS: True if any values were placed, otherwise false.
        RAISES: None.*/
        public bool ApplyNakedSingles()
        {
            bool progress = false;
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var tile = Tiles[row, col];
                    if (tile.Value == '0' && tile.IsSingleValue())
                    {
                        char value = GetSingleValue(tile.PossibleValuesBitmask);
                        if (IsValidMove(row, col, value))
                        {
                            tile.Value = value;
                            board.UpdateConstraints(row, col, value, false);
                            progress = true;
                            if (board.DebugMode)
                            {
                                Console.WriteLine($"Naked Single Applied at ({row}, {col}) with value {value}");
                            }
                        }
                    }
                }
            }
            return progress;
        }

       /* DOES: Extracts the single value from a bitmask when only one bit is set.
        ARGS: bitmask - The bitmask containing possible values.
        RETURNS: The single remaining value as a character.
        RAISES: None.*/
        private char GetSingleValue(long bitmask)
        {
            int value = (int)Math.Log2(bitmask);
            return (char)('0' + value);
        }

        /* DOES: Applies the Hidden Singles heuristic by finding hidden singles in rows, columns, and boxes.
         * ARGS:NONE
         RETURNS: true if any progress is made, otherwise false
        RAISES: none*/
        public bool ApplyHiddenSingles()
        {
            bool progress = false;
            for (int num = 1; num <= Size; num++)
            {
                long bitMask = 1L << num;

                for (int i = 0; i < Size; i++)
                {
                    progress |= FindHiddenSingle(i, bitMask, true) || FindHiddenSingle(i, bitMask, false);
                }

                // Check boxes for hidden singles
                int blockSize = (int)Math.Sqrt(Size);
                for (int boxRow = 0; boxRow < blockSize; boxRow++)
                {
                    for (int boxCol = 0; boxCol < blockSize; boxCol++)
                    {
                        progress |= FindHiddenSingleInBox(boxRow, boxCol, bitMask);
                    }
                }
            }
            return progress;
        }
        
        /* DOES: Finds hidden singles in a specific box and sets the value if found.
           ARGS: boxRow - The row index of the box.
                 boxCol - The column index of the box.
                 bitMask - The bitmask representing the possible value to find.
           RETURNS: True if a hidden single is found and set, otherwise false.
           RAISES: None. */
        private bool FindHiddenSingleInBox(int boxRow, int boxCol, long bitMask)
        {
            int startRow = boxRow * BlockSize;
            int startCol = boxCol * BlockSize;
            int posRow = -1, posCol = -1, count = 0;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    int row = startRow + r;
                    int col = startCol + c;
                    if (Tiles[row, col].Value == '0' && (Tiles[row, col].PossibleValuesBitmask & bitMask) != 0)
                    {
                        posRow = row;
                        posCol = col;
                        count++;
                    }
                }
            }
            if (count == 1)
            {
                char value = GetSingleValue(bitMask);
                if (IsValidMove(posRow, posCol, value))
                {
                    Tiles[posRow, posCol].Value = value;
                    board.UpdateConstraints(posRow, posCol, value, false);
                    return true;
                }
            }
            return false;
        }

        /* DOES: Finds hidden singles in a specific row or column and sets the value if found.
           ARGS: index - The index of the row or column.
                 bitMask - The bitmask representing the possible value to find.
                 isRow - True if searching in a row, false if searching in a column.
           RETURNS: True if a hidden single is found and set, otherwise false.
           RAISES: None. */
        private bool FindHiddenSingle(int index, long bitMask, bool isRow)
        {
            int pos = -1, count = 0;
            for (int i = 0; i < Size; i++)
            {
                int row = isRow ? index : i, col = isRow ? i : index;
                if (Tiles[row, col].Value == '0' && (Tiles[row, col].PossibleValuesBitmask & bitMask) != 0)
                {
                    pos = i;
                    count++;
                }
            }
            if (count == 1)
            {
                int row = isRow ? index : pos, col = isRow ? pos : index;
                char value = GetSingleValue(bitMask);
                if (IsValidMove(row, col, value))
                {
                    Tiles[row, col].Value = value;
                    board.UpdateConstraints(row, col, value, false);
                    return true;
                }
            }
            return false;
        }


        /* DOES: Applies the Naked Sets heuristic by identifying and eliminating naked sets in rows, columns, and boxes.
           ARGS: None.
           RETURNS: True if any values were eliminated, otherwise false.
           RAISES: None. */
        public bool ApplyNakedSets()
        {
            bool progress = false;
            for (int setSize = 2; setSize <= BlockSize; setSize++)
            {
                for (int i = 0; i < Size; i++)
                {
                    progress |= EliminateNakedSets(FindNakedSets(i, setSize, true, false)) ||
                                EliminateNakedSets(FindNakedSets(i, setSize, false, false)) ||
                                EliminateNakedSets(FindNakedSets(i, setSize, false, true));
                }
            }
            return progress;
        }

        /* DOES: Finds naked sets of a given size in rows, columns, or boxes.
           ARGS: index - The index of the row, column, or box.
                 setSize - The size of the naked set to find.
                 isRow - True if searching in a row, false if searching in a column.
                 isBox - True if searching in a box.
           RETURNS: A list of tuples containing the row, column, and bitmask of the naked sets found.
           RAISES: None. */
        private List<(int, int, long)> FindNakedSets(int index, int setSize, bool isRow, bool isBox)
        {
            var nakedSets = new List<(int, int, long)>();

            if (isBox)
            {
                int startRow = (index / BlockSize) * BlockSize, startCol = (index % BlockSize) * BlockSize;
                for (int i = 0; i < BlockSize; i++)
                {
                    for (int j = 0; j < BlockSize; j++)
                    {
                        int row = startRow + i, col = startCol + j;
                        var bitmask = Tiles[row, col].PossibleValuesBitmask;
                        if (Tiles[row, col].Value == '0' && board.CountSetBits(bitmask) <= setSize)
                        {
                            nakedSets.Add((row, col, bitmask));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < Size; i++)
                {
                    int row = isRow ? index : i, col = isRow ? i : index;
                    var bitmask = Tiles[row, col].PossibleValuesBitmask;
                    if (Tiles[row, col].Value == '0' && board.CountSetBits(bitmask) <= setSize)
                    {
                        nakedSets.Add((row, col, bitmask));
                    }
                }
            }

            return nakedSets;
        }

        /* DOES: Eliminates values from the possible values of tiles based on identified naked sets.
           ARGS: candidates - A list of tuples containing the row, column, and bitmask of the naked sets.
           RETURNS: True if any values were eliminated, otherwise false.
           RAISES: None. */
        private bool EliminateNakedSets(List<(int, int, long)> candidates)
        {
            var groups = candidates.GroupBy(c => c.Item3).Where(g => g.Count() == board.CountSetBits(g.Key));
            bool progress = false;
            foreach (var group in groups)
            {
                long mask = group.Key;
                foreach (var (row, col, bitmask) in candidates.Except(group))
                {
                    Tiles[row, col].PossibleValuesBitmask &= ~mask;
                    progress = true;
                }
            }
            return progress;
        }

        /* DOES: Applies the Simple Pairs heuristic by identifying and placing values for tiles with exactly two possible values.
           ARGS: None.
           RETURNS: True if any values were placed, otherwise false.
           RAISES: None. */
        public bool ApplySimplePairs()
        {
            bool progress = false;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0' && board.CountSetBits(Tiles[row, col].PossibleValuesBitmask) == 2)
                    {
                        foreach (char value in GetPossibleValues(Tiles[row, col].PossibleValuesBitmask))
                        {
                            if (IsValidMove(row, col, value))
                            {
                                Tiles[row, col].Value = value;
                                board.UpdateConstraints(row, col, value, false);
                                progress = true;
                                break;
                            }
                        }
                    }
                }
            }
            return progress;
        }

        /* DOES: Retrieves the possible values for a tile based on its bitmask.
           ARGS: bitmask - The bitmask containing possible values.
           RETURNS: An enumerable of characters representing the possible values.
           RAISES: None. */
        private IEnumerable<char> GetPossibleValues(long bitmask)
        {
            return Enumerable.Range(1, Size)
                .Where(i => (bitmask & (1L << i)) != 0)
                .Select(i => (char)('0' + i));
        }

        /* DOES: Checks if placing a value in a specific tile is a valid move.
           ARGS: row - The row index of the tile.
                 col - The column index of the tile.
                 value - The value to place in the tile.
           RETURNS: True if the move is valid, otherwise false.
           RAISES: None. */
        public bool IsValidMove(int row, int col, char value)
        {
            Tiles[row, col].Value = value;
            bool isValid = board.IsValid();
            Tiles[row, col].Value = '0';
            return isValid;
        }
    }
}