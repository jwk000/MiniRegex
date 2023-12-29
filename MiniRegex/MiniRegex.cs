using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniRegex
{


    class MiniRegex
    {
        PatternExp Exp = new PatternExp();
        public string Pattern { get; private set; }
        public MiniRegex(string pattern)
        {
            Pattern = pattern;
            if (!Parse(pattern))
            {
                throw new ArgumentException("invalid pattern");
            }
        }

        bool Parse(string pattern)
        {
            return Exp.Parse(new StringReader(pattern));
        }

        public bool IsMatch(string input)
        {
            return Exp.Match(new StringReader(input));
        }
    }

}
