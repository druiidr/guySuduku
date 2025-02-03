using System;

namespace guy_s_sudoku
{
    internal class Heuristic
    {
        private Tile[,] Tiles;
        private int Size;

        public Heuristic(Tile[,] tiles, int size)
        {
            Tiles = tiles;
            Size = size;
        }

        public void ApplyAll()
        {
            bool progress;
            do
            {
                progress = false;
                progress |= ApplyNakedSingles();
                progress |= ApplyHiddenSingles();
                progress |= ApplyNakedSets();
                progress |= ApplySimplePairs();
            } while (progress);
        }

        public bool ApplyNakedSingles()
        {
            bool progress = false;
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (Tiles[row, col].Value == '0' && Tiles[row, col].IsSingleValue())
                    {
                        Tiles[row, col].Value = (char)('0' + GetSingleValue(Tiles[row, col].PossibleValuesBitmask));
                        UpdateConstraints(row, col);
                        progress = true;
                    }
                }
            }
            return progress;
        }

        public bool ApplyHiddenSingles()
        {
            bool progress = false;
            int blockSize = (int)Math.Sqrt(Size);
            for (int num = 1; num <= Size; num++)
            {
                long bitMask = 1L << num;

                for (int i = 0; i < Size; i++)
                {
                    progress |= FindHiddenSingleInGroup(i, bitMask, true);  // Rows
                    progress |= FindHiddenSingleInGroup(i, bitMask, false); // Columns
                    progress |= FindHiddenSingleInBox(i, bitMask, blockSize); // Boxes
                }
            }
            return progress;
        }

        private bool FindHiddenSingleInGroup(int index, long bitMask, bool isRow)
        {
            int pos = -1, count = 0;
            for (int i = 0; i < Size; i++)
            {
                int row = isRow ? index : i;
                int col = isRow ? i : index;
                if (Tiles[row, col].Value == '0' && (Tiles[row, col].PossibleValuesBitmask & bitMask) != 0)
                {
                    pos = i;
                    count++;
                }
            }
            if (count == 1)
            {
                int row = isRow ? index : pos;
                int col = isRow ? pos : index;
                Tiles[row, col].Value = (char)('0' + (int)Math.Log2(bitMask));
                UpdateConstraints(row, col);
                return true;
            }
            return false;
        }

        private bool FindHiddenSingleInBox(int boxIndex, long bitMask, int blockSize)
        {
            int startRow = (boxIndex / blockSize) * blockSize;
            int startCol = (boxIndex % blockSize) * blockSize;
            int posRow = -1, posCol = -1, count = 0;

            for (int i = 0; i < blockSize; i++)
            {
                for (int j = 0; j < blockSize; j++)
                {
                    int row = startRow + i, col = startCol + j;
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
                Tiles[posRow, posCol].Value = (char)('0' + (int)Math.Log2(bitMask));
                UpdateConstraints(posRow, posCol);
                return true;
            }
            return false;
        }

        public bool ApplyNakedSets()
        {
            bool progress = false;
            int blockSize = (int)Math.Sqrt(Size);

            for (int setSize = 2; setSize <= blockSize; setSize++)
            {
                for (int i = 0; i < Size; i++)
                {
                    progress |= FindNakedSetsInGroup(i, setSize, true);
                    progress |= FindNakedSetsInGroup(i, setSize, false);
                    progress |= FindNakedSetsInBox(i, setSize, blockSize);
                }
            }
            return progress;
        }

        private bool FindNakedSetsInGroup(int index, int setSize, bool isRow)
        {
            List<(int, long)> candidates = new();
            for (int i = 0; i < Size; i++)
            {
                int row = isRow ? index : i;
                int col = isRow ? i : index;
                if (Tiles[row, col].Value == '0')
                {
                    long bitmask = Tiles[row, col].PossibleValuesBitmask;
                    if (CountBits(bitmask) <= setSize)
                    {
                        candidates.Add((isRow ? col : row, bitmask));
                    }
                }
            }

            return EliminateNakedSets(candidates, setSize, isRow ? index : -1, isRow ? -1 : index);
        }

        private bool FindNakedSetsInBox(int boxIndex, int setSize, int blockSize)
        {
            List<(int, int, long)> candidates = new();
            int startRow = (boxIndex / blockSize) * blockSize;
            int startCol = (boxIndex % blockSize) * blockSize;

            for (int i = 0; i < blockSize; i++)
            {
                for (int j = 0; j < blockSize; j++)
                {
                    int row = startRow + i, col = startCol + j;
                    if (Tiles[row, col].Value == '0')
                    {
                        long bitmask = Tiles[row, col].PossibleValuesBitmask;
                        if (CountBits(bitmask) <= setSize)
                        {
                            candidates.Add((row, col, bitmask));
                        }
                    }
                }
            }
            return EliminateNakedSets(candidates, setSize);
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
                            Tiles[row, col].Value = value;
                            UpdateConstraints(row, col);
                            if (!IsValid())
                            {
                                Tiles[row, col].Value = '0';
                                UpdateConstraints(row, col); // Restore previous constraints
                            }
                            else
                            {
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
            for (int i = 1; i <= Size; i++)
            {
                if ((bitmask & (1L << i)) != 0)
                {
                    yield return (char)('0' + i);
                }
            }
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


        private int CountBits(long bitmask) => Convert.ToString(bitmask, 2).Count(c => c == '1');

        private bool EliminateNakedSets(List<(int, long)> candidates, int setSize, int row, int col)
        {
            var groups = candidates.GroupBy(c => c.Item2).Where(g => g.Count() == setSize).ToList();
            if (!groups.Any()) return false;

            bool progress = false;
            foreach (var group in groups)
            {
                long mask = group.Key;
                foreach (var (i, _) in candidates.Except(group))
                {
                    if (row != -1 && col == -1)
                    {
                        Tiles[row, i].PossibleValuesBitmask &= ~mask;
                    }
                    else if (row == -1 && col != -1)
                    {
                        Tiles[i, col].PossibleValuesBitmask &= ~mask;
                    }
                    progress = true;
                }
            }
            return progress;
        }

        private bool EliminateNakedSets(List<(int, int, long)> candidates, int setSize)
        {
            var groups = candidates.GroupBy(c => c.Item3).Where(g => g.Count() == setSize).ToList();
            if (!groups.Any()) return false;

            bool progress = false;
            foreach (var group in groups)
            {
                long mask = group.Key;
                foreach (var (r, c, _) in candidates.Except(group))
                {
                    Tiles[r, c].PossibleValuesBitmask &= ~mask;
                    progress = true;
                }
            }
            return progress;
        }
        private void UpdateConstraints(int row, int col)
        {
            char num = Tiles[row, col].Value;
            long bitMask = 1L << (num - '0');
            int blockSize = (int)Math.Sqrt(Size);

            // Update row and column constraints
            for (int i = 0; i < Size; i++)
            {
                if (Tiles[row, i].Value == '0')
                {
                    Tiles[row, i].PossibleValuesBitmask &= ~bitMask;
                }
                if (Tiles[i, col].Value == '0')
                {
                    Tiles[i, col].PossibleValuesBitmask &= ~bitMask;
                }
            }

            // Update box constraints
            int startRow = (row / blockSize) * blockSize;
            int startCol = (col / blockSize) * blockSize;
            for (int i = 0; i < blockSize; i++)
            {
                for (int j = 0; j < blockSize; j++)
                {
                    if (Tiles[startRow + i, startCol + j].Value == '0')
                    {
                        Tiles[startRow + i, startCol + j].PossibleValuesBitmask &= ~bitMask;
                    }
                }
            }
        }

        private int GetSingleValue(long bitmask)
        {
            for (int i = 1; i <= Size; i++)
            {
                if ((bitmask & (1L << i)) != 0)
                {
                    return i;
                }
            }
            throw new InvalidOperationException("No single value found in bitmask.");
        }
    }
}

