using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Heuristic
    {
        private const int MaxIterations = 10000;
        private const double AcceptedEmptySpacePercentage = 0.7;
        private const int SmallBoardSideSize = 4;
        private const int MediumBoardSideSize = 9;
        private readonly Tile[,] Tiles;
        private readonly int Size;
        private readonly int BlockSize;
        private readonly Board board; // Reference to Board class

        /// <summary>
        /// Heuristic constructor to initialize the tiles, size and board.
        /// </summary>
        /// <param name="tiles">The tiles of the board.</param>
        /// <param name="size">The size of the board.</param>
        /// <param name="board">The board instance.</param>
        public Heuristic(Tile[,] tiles, int size, Board board)
        {
            Tiles = tiles;
            Size = size;
            BlockSize = (int)Math.Sqrt(size);
            this.board = board; // Initialize Board reference
        }

        /// <summary>
        /// Apply all the heuristics.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
        public bool ApplyAll()
        {
            bool progress;
            int iterations = 0;

            do
            {
                progress = ApplyHeuristics();
                iterations++;

                if (board.DebugMode)
                {
                    Console.WriteLine($"Iteration {iterations}, Progress: {progress}");
                    board.PrintBoard(); // Print board state after each iteration
                }

                if (iterations > MaxIterations)
                {
                    Console.WriteLine("Maximum iterations reached. Exiting to prevent infinite loop.");
                    break;
                }

            } while (progress && !board.IsSolved());

            return progress;
        }

        /// <summary>
        /// Apply heuristics based on the current state of the board.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
        private bool ApplyHeuristics()
        {
            bool progress = false;
            int emptyCellsCount = board.CountEmptyCells();

            // Apply heuristics based on the current state of the board
            var watch = Stopwatch.StartNew();
            if (emptyCellsCount > Size * Size * AcceptedEmptySpacePercentage) // Mostly empty board
            {
                progress |= ApplyHiddenSets() || ApplyNakedSets(); // Apply more complex heuristics first
            }
            else if (emptyCellsCount < Size * Size * (1 - AcceptedEmptySpacePercentage)) // mostly full board
            {
                progress |= ApplyNakedSingles() || ApplyHiddenSingles(); // Apply simpler heuristics first
            }
            else
            {
                progress |= ApplyNakedSingles() || ApplyHiddenSingles() || ApplyNakedSets() || ApplyHiddenSets();
            }
            watch.Stop();
            if (board.DebugMode)
            {
                Console.WriteLine($"Heuristics Time: {watch.ElapsedMilliseconds} ms");
                if (progress) board.PrintBoard();
            }

            watch.Restart();
            progress |= ApplySimplePairs();
            watch.Stop();
            if (board.DebugMode)
            {
                Console.WriteLine($"Simple Pairs Time: {watch.ElapsedMilliseconds} ms");
                if (progress) board.PrintBoard();
            }

            return progress;
        }

        /// <summary>
        /// Apply naked singles heuristic.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
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

        /// <summary>
        /// Isolate the single value from the bitmask.
        /// </summary>
        /// <param name="bitmask">The bitmask representing possible values.</param>
        /// <returns>The isolated single value.</returns>
        private char GetSingleValue(long bitmask)
        {
            int value = (int)Math.Log2(bitmask);
            return (char)('0' + value);
        }

        /// <summary>
        /// Apply hidden singles heuristic.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
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

        /// <summary>
        /// Find hidden single in the box.
        /// </summary>
        /// <param name="boxRow">The row index of the box.</param>
        /// <param name="boxCol">The column index of the box.</param>
        /// <param name="bitMask">The bitmask representing possible values.</param>
        /// <returns>True if a hidden single was found, otherwise false.</returns>
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

        /// <summary>
        /// Find hidden single in the row or column.
        /// </summary>
        /// <param name="index">The index of the row or column.</param>
        /// <param name="bitMask">The bitmask representing possible values.</param>
        /// <param name="isRow">True if searching in a row, otherwise false.</param>
        /// <returns>True if a hidden single was found, otherwise false.</returns>
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

        /// <summary>
        /// Apply naked sets heuristic.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
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

        /// <summary>
        /// Find naked sets according to the given parameters.
        /// </summary>
        /// <param name="index">The index of the row or column.</param>
        /// <param name="setSize">The size of the set.</param>
        /// <param name="isRow">True if searching in a row, otherwise false.</param>
        /// <param name="isBox">True if searching in a box, otherwise false.</param>
        /// <returns>A list of naked sets.</returns>
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

        /// <summary>
        /// Eliminate naked sets.
        /// </summary>
        /// <param name="candidates">The list of candidates.</param>
        /// <returns>True if progress was made, otherwise false.</returns>
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

        /// <summary>
        /// Apply hidden sets heuristic.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
        public bool ApplyHiddenSets()
        {
            bool progress = false;
            for (int setSize = 2; setSize <= BlockSize; setSize++)
            {
                for (int i = 0; i < Size; i++)
                {
                    progress |= EliminateHiddenSets(FindHiddenSets(i, setSize, true, false)) ||
                                EliminateHiddenSets(FindHiddenSets(i, setSize, false, false)) ||
                                EliminateHiddenSets(FindHiddenSets(i, setSize, false, true));
                }
            }
            return progress;
        }

        /// <summary>
        /// Find hidden sets according to the given parameters.
        /// </summary>
        /// <param name="index">The index of the row or column.</param>
        /// <param name="setSize">The size of the set.</param>
        /// <param name="isRow">True if searching in a row, otherwise false.</param>
        /// <param name="isBox">True if searching in a box, otherwise false.</param>
        /// <returns>A list of hidden sets.</returns>
        private List<(int, int, long)> FindHiddenSets(int index, int setSize, bool isRow, bool isBox)
        {
            var hiddenSets = new List<(int, int, long)>();

            if (isBox)
            {
                int startRow = (index / BlockSize) * BlockSize, startCol = (index % BlockSize) * BlockSize;
                for (int i = 0; i < BlockSize; i++)
                {
                    for (int j = 0; j < BlockSize; j++)
                    {
                        int row = startRow + i, col = startCol + j;
                        var bitmask = Tiles[row, col].PossibleValuesBitmask;
                        if (Tiles[row, col].Value == '0')
                        {
                            hiddenSets.Add((row, col, bitmask));
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
                    if (Tiles[row, col].Value == '0')
                    {
                        hiddenSets.Add((row, col, bitmask));
                    }
                }
            }

            return hiddenSets;
        }

        /// <summary>
        /// Eliminate hidden sets.
        /// </summary>
        /// <param name="candidates">The list of candidates.</param>
        /// <returns>True if progress was made, otherwise false.</returns>
        private bool EliminateHiddenSets(List<(int, int, long)> candidates)
        {
            var candidateBitmasks = candidates.Select(c => c.Item3).ToList();
            bool progress = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                var (row1, col1, bitmask1) = candidates[i];
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    var (row2, col2, bitmask2) = candidates[j];
                    long combinedMask = bitmask1 | bitmask2;
                    if (board.CountSetBits(combinedMask) == 2 && candidateBitmasks.Count(bm => (bm & combinedMask) != 0) == 2)
                    {
                        Tiles[row1, col1].PossibleValuesBitmask &= combinedMask;
                        Tiles[row2, col2].PossibleValuesBitmask &= combinedMask;
                        progress = true;
                    }
                }
            }

            return progress;
        }

        /// <summary>
        /// Apply simple pairs heuristic.
        /// </summary>
        /// <returns>True if progress was made, otherwise false.</returns>
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

        /// <summary>
        /// Get the possible values from the bitmask.
        /// </summary>
        /// <param name="bitmask">The bitmask representing possible values.</param>
        /// <returns>An enumerable of possible values.</returns>
        private IEnumerable<char> GetPossibleValues(long bitmask)
        {
            return Enumerable.Range(1, Size)
                .Where(i => (bitmask & (1L << i)) != 0)
                .Select(i => (char)('0' + i));
        }

        /// <summary>
        /// Check if the move is valid.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="col">The column index.</param>
        /// <param name="value">The value to be placed.</param>
        /// <returns>True if the move is valid, otherwise false.</returns>
        public bool IsValidMove(int row, int col, char value)
        {
            Tiles[row, col].Value = value;
            bool isValid = board.IsValidInput();
            Tiles[row, col].Value = '0';
            return isValid;
        }
    }
}