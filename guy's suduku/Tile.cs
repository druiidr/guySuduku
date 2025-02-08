namespace guy_s_sudoku
{
    internal class Tile
    {
        public char Value { get; set; }
        public long PossibleValuesBitmask { get; set; }

        public Tile()
        {
            Value = '0';
            PossibleValuesBitmask = (1L << 10) - 2; // All bits set except the least significant bit (0 is not a possible value)
        }

        public bool IsSingleValue()
        {
            return CountBits(PossibleValuesBitmask) == 1;
        }

        private int CountBits(long bitmask)
        {
            int count = 0;
            while (bitmask != 0)
            {
                count++;
                bitmask &= (bitmask - 1);
            }
            return count;
        }
    }
}
