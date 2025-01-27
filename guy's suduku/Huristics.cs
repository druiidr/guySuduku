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
