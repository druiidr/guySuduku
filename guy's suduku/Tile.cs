using System.Collections.Generic;
using System.Linq;

namespace guy_s_sudoku
{
    internal class Tile
    {
        public int Value { get; set; }
        public HashSet<int> PossibleValues { get; set; }
        public Tile()
        {
            Value = 0;
            PossibleValues = new HashSet<int>(Enumerable.Range(1, 9));
        }
    }
}
