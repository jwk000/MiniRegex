using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniRegex
{
    class StringReader
    {
        public string Input { get; private set; }
        public int Index { get; private set; }
        public StringReader(string input) { Input = input; }
        public StringReader Clone()
        {
            var ret = new StringReader(Input);
            ret.Index = Index;
            return ret;
        }
        public void CopyFrom(StringReader sr)
        {
            Input = sr.Input;
            Index = sr.Index;
        }
        public char Read()
        {
            if (Index >= Input.Length)
            {
                return char.MinValue;
            }
            return Input[Index++];
        }

        public char Peek()
        {
            if (Index >= Input.Length)
            {
                return char.MinValue;
            }
            return Input[Index];
        }
        public char ReadBack(int n)
        {
            int idx = Index - n;
            if (idx < 0)
            {
                return char.MinValue;
            }
            return Input[idx];
        }
        public void RollBack(int n = 1)
        {
            Index -= n;
        }
        public void Forward(int n = 1)
        {
            Index += n;
        }

        public bool EOF()
        {
            return Index >= Input.Length;
        }
        public string ReadFromTo(int from, int to)
        {
            if (from >= 0 && from < to)
            {
                return Input.Substring(from, to - from);
            }
            return null;
        }
        public string ReadTo(char C)//包含index不包含C
        {
            int idx = Index;
            while (idx++ < Input.Length - 1)
            {
                if (Input[idx] == C)
                {
                    string ans = Input.Substring(Index, idx - Index);
                    Index = idx + 1;
                    return ans;
                }
            }
            return null;
        }

        public string ReadToEnd()
        {
            return Input.Substring(Index);
        }

        public string ReadMatch(char left, char right)
        {
            int depth = 1;
            int idx = Index;
            while (idx++ < Input.Length - 1)
            {
                char c = Input[idx];
                if (c == left)
                {
                    depth++;
                }
                else if (c == right)
                {
                    depth--;
                    if (depth == 0)
                    {
                        string ans = Input.Substring(Index, idx - Index);
                        Index = idx + 1;
                        return ans;
                    }
                }
            }

            return null;
        }

    }

}
