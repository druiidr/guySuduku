using System.Collections.Generic;

namespace guy_s_sudoku
{
    internal class Tile
    {
        public char Value { get; set; }
        public long PossibleValuesBitmask { get; set; }

        public Tile()
        {
            Value = '0';
            PossibleValuesBitmask = (1L << 31) - 2; // All values (1-30) possible initially
        }

        public bool IsSingleValue()
        {
            return (PossibleValuesBitmask & (PossibleValuesBitmask - 1)) == 0;
        }
    }
}
