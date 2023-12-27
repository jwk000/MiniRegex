using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFARegex
{
    [Flags]
    enum CharFlag
    {
        LineBegin = 1,
        LineEnd = 2,
        LeftBoundary = 3,
        RightBoundary = 4,
    }
}
