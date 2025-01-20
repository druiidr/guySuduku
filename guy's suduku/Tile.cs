using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace guy_s_suduku
{
    internal class Tile
    {
            public int Value { get; set; } // The current value of the cell (0 if unsolved)
            public HashSet<int> PossibleValues { get; set; } // Possible values for this cell

            public Tile()
            {
                Value = 0;
                PossibleValues = new HashSet<int>(Enumerable.Range(1, Constants.SQUARE_PARAMS));
            }
        }
    }
