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
            const int maxIterations = 1000; // Adjust as needed

            do
            {
                progress = ApplyHeuristics();
                iterations++;

                if (iterations > maxIterations)
                {
                    Console.WriteLine("Maximum iterations reached. Exiting to prevent infinite loop.");
                    break;
                }

            } while (progress && !board.IsSolved());

            if (!board.IsSolved())
            {
                var emptyCells = board.GetEmptyCells();
                if (!board.BacktrackSolve(emptyCells, 0))
                {
                    Console.WriteLine("No solution exists.");
                    return false;
                }
            }

            return true;
        }


        private bool ApplyHeuristics()
        {
            bool progress = false;

            // Start with simpler heuristics
            progress = ApplyNakedSingles() || ApplyHiddenSingles();

            // If simple heuristics don't progress much, use more advanced ones dynamically
            if (!progress || board.CountEmptyCells() < GetAdaptiveThreshold())
            {
                progress |= ApplyNakedSets() || ApplySimplePairs();
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
                            UpdateConstraints(row, col);
                            progress = true;
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
            }
            return progress;
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
                    UpdateConstraints(row, col);
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
                        if (Tiles[row, col].Value == '0' && CountBits(bitmask) <= setSize)
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
                    if (Tiles[row, col].Value == '0' && CountBits(bitmask) <= setSize)
                    {
                        nakedSets.Add((row, col, bitmask));
                    }
                }
            }

            return nakedSets;
        }

        private bool EliminateNakedSets(List<(int, int, long)> candidates)
        {
            var groups = candidates.GroupBy(c => c.Item3).Where(g => g.Count() == CountBits(g.Key));
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
                    if (Tiles[row, col].Value == '0' && CountBits(Tiles[row, col].PossibleValuesBitmask) == 2)
                    {
                        foreach (char value in GetPossibleValues(Tiles[row, col].PossibleValuesBitmask))
                        {
                            if (IsValidMove(row, col, value))
                            {
                                Tiles[row, col].Value = value;
                                UpdateConstraints(row, col);
                                progress = true;
                                break;
                            }
                        }
                    }
                }
            }
            return progress;
        }


        private void UpdateConstraints(int row, int col)
        {
            long bitMask = 1L << (Tiles[row, col].Value - '0');

            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0') Tiles[row, i].PossibleValuesBitmask &= ~bitMask;
                if (Tiles[i, col].Value == '0') Tiles[i, col].PossibleValuesBitmask &= ~bitMask;
            }

            int startRow = (row / BlockSize) * BlockSize;
            int startCol = (col / BlockSize) * BlockSize;

            for (int r = 0; r < BlockSize; r++)
            {
                for (int c = 0; c < BlockSize; c++)
                {
                    int currentRow = startRow + r;
                    int currentCol = startCol + c;
                    if (currentRow != row && currentCol != col && Tiles[currentRow, currentCol].Value == '0')
                        Tiles[currentRow, currentCol].PossibleValuesBitmask &= ~bitMask;
                }
            }
        }

        private bool IsValueConflicting(int row, int col, char value)
        {
            long bitMask = 1L << (value - '0');
            return (Tiles[row, col].PossibleValuesBitmask & bitMask) == 0;
        }

        private int CountBits(long bitmask) => Convert.ToString(bitmask, 2).Count(c => c == '1');

        private IEnumerable<char> GetPossibleValues(long bitmask)
        {
            return Enumerable.Range(1, Size)
                .Where(i => (bitmask & (1L << i)) != 0)
                .Select(i => (char)('0' + i));
        }


        private bool IsValidMove(int row, int col, char value)
        {
            Tiles[row, col].Value = value;
            bool isValid = IsValid();
            Tiles[row, col].Value = '0';
            return isValid;
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

    }
}
