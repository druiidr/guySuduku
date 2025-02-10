using System.Numerics;

namespace guy_s_sudoku
{
    /* DOES: Represents a tile in the Sudoku board with a value and possible values bitmask.
       ARGS: None.
       RETURNS: None.
       RAISES: None. */
    internal class Tile
    {
        public char Value { get; set; }
        public long PossibleValuesBitmask { get; set; }

        /* DOES: Initializes a new instance of the Tile class with default values.
           ARGS: None.
           RETURNS: None.
           RAISES: None. */
        public Tile()
        {
            Value = '0';
            PossibleValuesBitmask = (1L << 10) - 2; // All bits set except the least significant bit (0 is not a possible value)
        }

        /* DOES: Checks if the tile has only one possible value.
           ARGS: None.
           RETURNS: True if the tile has only one possible value, otherwise false.
           RAISES: None. */
        public bool IsSingleValue()
        {
            return CountSetBits(PossibleValuesBitmask) == 1;
        }

        /* DOES: Counts the number of set bits in a bitmask.
           ARGS: bitmask - The bitmask to count set bits in.
           RETURNS: The number of set bits in the bitmask.
           RAISES: None. */
        private int CountSetBits(long bitmask)
        {
            return BitOperations.PopCount((ulong)bitmask);
        }
    }
}
