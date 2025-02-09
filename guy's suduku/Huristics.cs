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
        private readonly Board board; // Reference to Board class

        public Heuristic(Tile[,] tiles, int size, Board board)
        {
            Tiles = tiles;
            Size = size;
            BlockSize = (int)Math.Sqrt(size);
            this.board = board; // Initialize Board reference
        }

        public bool ApplyAll()
        {
            bool progress;
            int iterations = 0;
            const int maxIterations = 1000;

            do
            {
                progress = ApplyHeuristics();
                iterations++;

                if (board.DebugMode)
                {
                    Console.WriteLine($"Iteration {iterations}, Progress: {progress}");
                }

                if (iterations > maxIterations)
                {
                    Console.WriteLine("Maximum iterations reached. Exiting to prevent infinite loop.");
                    break;
                }

            } while (progress && !board.IsSolved());

            return progress;
        }


        private bool ApplyHeuristics()
        {
            bool progress = false;

            // Start with simpler heuristics
            progress = ApplyNakedSingles() || ApplyHiddenSingles();

            if (board.DebugMode)
            {
                Console.WriteLine("After applying Naked Singles and Hidden Singles:");
                board.PrintBoard();
            }

            // If simple heuristics don't progress much, use more advanced ones dynamically
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

        private int GetAdaptiveThreshold()
        {
            // Set adaptive threshold based on board size
            if (Size <= 4) return 5; // Smaller boards
            if (Size <= 9) return 20; // Medium boards
            return 30; // Larger boards
        }

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


        private char GetSingleValue(long bitmask)
        {
            int value = (int)Math.Log2(bitmask);
            return (char)('0' + value);
        }

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

        private IEnumerable<char> GetPossibleValues(long bitmask)
        {
            return Enumerable.Range(1, Size)
                .Where(i => (bitmask & (1L << i)) != 0)
                .Select(i => (char)('0' + i));
        }

        private bool IsValidMove(int row, int col, char value)
        {
            Tiles[row, col].Value = value;
            bool isValid = board.IsValid();
            Tiles[row, col].Value = '0';
            return isValid;
        }
    }
}