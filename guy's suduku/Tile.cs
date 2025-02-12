using System.Numerics;

namespace guy_s_sudoku
{
    /// <summary>
    /// Represents a single tile on the Sudoku board.
    /// </summary>
    internal class Tile
    {
        public char Value { get; set; }
        public long PossibleValuesBitmask { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tile"/> class.
        /// </summary>
        public Tile()
        {
            Value = '0';
            PossibleValuesBitmask = (1L << 10) - 2; // All bits set except the least significant bit (0 is not a possible value)
        }

        /// <summary>
        ///  Sets the value of the tile.
        /// </summary>
        /// <returns>true if has 1 possible value</returns>
        public bool IsSingleValue()
        {
            return CountSetBits(PossibleValuesBitmask) == 1;
        }

        /// <summary>
        /// Counts the number of set bits in the bitmask.
        /// </summary>
        /// <param name="bitmask"></param>
        /// <returns>int: the count of set bits</returns>
        private int CountSetBits(long bitmask)
        {
            return BitOperations.PopCount((ulong)bitmask);
        }
    }
}
