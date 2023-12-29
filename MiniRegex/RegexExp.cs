using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniRegex
{
    /************实现一个正则表达式的子集************************************************
    *
    * \d 数字[0-9]
    * \D 非数字
    * \w 单词 数字字母下划线 [0-9a-zA-Z_]
    * \W 非单词
    * \s 空白 [ \t\r\n\f\v]
    * \S 非空白
    * [abc] 字符集
    * [^abc] 逆字符集
    * [a-g] 区间字符集
    * X{m} 量词 m个X，贪婪
    * X{m,n} m-n个X，贪婪
    * X{m,} 至少m个X，贪婪
    * () 分组并捕获
    * |  或 'z|food' 能匹配 "z" 或 "food"。'(z|f)ood' 则匹配 "zood" 或 "food"
    * .  任意字符，不含\n
    * X*  任意多个X {0,} 贪婪 A\s*=\s*B
    * X+  至少一个X {1,} 贪婪
    * X?  0到1个X {0,1} 贪婪
    * X*? X+? X?? X{}? 非贪婪
    * ^  行首
    * $  行尾不含\n
    * 
    * ******************************************************************************
    *
    * EBNF描述语法规则
    * pattern : alter
    * alter:  term ['|' alter]
    * term: atom [affix] [term+] 
    * atom : '(' group ')' | '[' charset ']' | meta 
    * group : alter
    * charset: char '-' char | meta+ | '^' charset
    * meta : '.' |'\d'|'\s'|'\w'|'\t'|'\r'|'\n'|'\b'|'^'|'$'|char
    * affix : '+'|'*'|'?'| '{' m[ ',' n] '}' 
    * 
    ********************************************************************************/

    enum MetaType
    {
        Any,
        Digit,
        NDigit,
        Space,
        NSpace,
        Word,
        NWord,
        Range,
        Char
    }

    [Flags]
    enum CharFlag
    {
        Normal = 0,
        LineBegin = 1,
        LineEnd = 2,
        LeftBoundary = 3,
        RightBoundary = 4,
        NBound = 5,//非边界
    }

    interface IExp
    {
        bool Parse(StringReader stringReader);
        bool Match(StringReader input);
        NFAFrag ToNFA();
        
    }

    abstract class AExp : IExp
    {
        public string PatternString { get; private set; }
        public override string ToString() { return PatternString; }
        public bool Parse(StringReader stringReader)
        {
            int idx1 = stringReader.Index;
            bool ret = ImplParse(stringReader);
            int idx2 = stringReader.Index;
            PatternString = stringReader.ReadFromTo(idx1, idx2);
            return ret;
        }

        protected virtual bool ImplParse(StringReader stringReader) { return false; }
        public virtual bool Match(StringReader input) { return false; }
        public virtual NFAFrag ToNFA() { return null; }
    }

    class PatternExp : AExp
    {
        AlterExp alter;
        protected override bool ImplParse(StringReader stringReader)
        {
            alter = new AlterExp();
            bool ret = alter.Parse(stringReader);
            return ret;
        }

        public override bool Match(StringReader input)
        {
            if (alter.Match(input))
            {
                return input.EOF();//匹配结束了才算成功
            }
            return false;
        }

        public override NFAFrag ToNFA()
        {
            return alter.ToNFA();
        }
    }

    class AlterExp : AExp
    {
        public TermExp term;
        public AlterExp alter;

        protected override bool ImplParse(StringReader stringReader)
        {
            term = new TermExp();
            string left = stringReader.ReadTo('|');
            if (left == null)
            {
                return term.Parse(stringReader);
            }
            else
            {
                if (!term.Parse(new StringReader(left)))
                {
                    return false;
                }
                alter = new AlterExp();
                return alter.Parse(stringReader);
            }
        }

        public override bool Match(StringReader input)
        {
            var clone = input.Clone();
            if (term.Match(input))
            {
                return true;
            }
            if (alter != null)
            {
                input.CopyFrom(clone);
                return alter.Match(input);
            }
            return false;
        }

        public override NFAFrag ToNFA()
        {
            NFAFrag frag = new NFAFrag();

            NFAFrag ret = term.ToNFA();
            frag.Start.Add(ret.Start);
            ret.End.Add(frag.End);
            if (alter != null)
            {
                ret = alter.ToNFA();
                frag.Start.Add(ret.Start);
                ret.End.Add(frag.End);
            }
            return frag;
        }
    }

    class TermExp : AExp
    {
        public AtomExp atom;
        public int from = 1, to = 1, max = 0;
        public TermExp term;

        protected override bool ImplParse(StringReader stringReader)
        {
            atom = new AtomExp();
            if (!atom.Parse(stringReader))
            {
                return false;
            }
            char c = stringReader.Peek();
            do
            {
                if (c == '+')
                {
                    stringReader.Read();
                    from = 1;
                    to = -1;
                    break;
                }
                if (c == '*')
                {
                    stringReader.Read();
                    from = 0;
                    to = -1;
                    break;
                }
                if (c == '?')
                {
                    stringReader.Read();
                    from = 0;
                    to = 1;
                    break;
                }
                if (c == '{')
                {
                    stringReader.Read();
                    string s = stringReader.ReadTo('}');
                    if (s == null)
                    {
                        return false;
                    }
                    StringReader sr = new StringReader(s);
                    string m = sr.ReadTo(',');
                    if (m == null)
                    {
                        from = int.Parse(s);//{2}
                        to = from;
                    }
                    else
                    {
                        from = int.Parse(m);
                        string n = sr.ReadToEnd();
                        if (string.IsNullOrEmpty(n))
                        {
                            to = -1; //{2,}
                        }
                        else
                        {
                            to = int.Parse(n);//{2,3}
                        }
                    }
                    break;
                }

            } while (false);
            if (to != -1 && from > to)
            {
                return false;
            }
            if (stringReader.EOF()) return true;
            term = new TermExp();
            return term.Parse(stringReader);
        }

        public override bool Match(StringReader input)
        {
            if (from > 0)
            {
                for (int i = 0; i < from; i++)
                {
                    if (!atom.Match(input))
                    {
                        return false;
                    }
                }
            }
            //贪婪匹配，计算实际匹配的最大数量
            max = from;
            while (!input.EOF() && (to == -1 || max < to))
            {
                int idx = input.Index;
                if (atom.Match(input))
                {
                    max++;
                }
                else
                {
                    input.SetIndex(idx);
                    break;
                }
            }

            if (term == null)
            {
                return true;
            }
            else
            {
                //从后往前回溯，超过from的部分都可以回退
                do
                {
                    var clone = input.Clone();
                    if (term.Match(clone))
                    {
                        input.CopyFrom(clone);
                        return true;
                    }
                    if (max-- > from)
                    {
                        input.RollBack();
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            }
            return false;
        }

        public override NFAFrag ToNFA()
        {
            NFAFrag frag = new NFAFrag();

            //atom部分
            NFAFrag atomFrag = atom.ToNFA();

            //term部分
            NFAState termStart;
            NFAState termEnd;
            if (term != null)
            {
                var termFrag = term.ToNFA();
                termStart = termFrag.Start;
                termEnd = termFrag.End;
            }
            else
            {
                termStart = new NFAState();
                termEnd = termStart;
            }

            NFAFrag atomClone = atomFrag.Clone();
            frag.Start.Add(atomClone.Start);
            atomClone.End.Add(frag.End);

            //处理量词
            if (from == 0)
            {
                frag.Start.Add(termStart);
                if (to == -1) //{0,-1}
                {
                    frag.End.Add(frag.Start);
                    frag.End.Add(termStart);
                }
                else //{0,3}
                {
                    for (int i = 0; i < to - 1; i++)
                    {
                        var next = atomFrag.Clone();
                        frag.End.Add(next.Start);
                        frag.End.Add(termStart);
                        frag.End = next.End;
                    }
                    frag.End.Add(termStart);
                }
            }
            else
            {
                NFAFrag lastFrag = frag;
                //已经有一个了，所以从1开始
                for (int i = 1; i < from; i++)
                {
                    var next = atomFrag.Clone();
                    frag.End.Add(next.Start);
                    frag.End = next.End;
                    lastFrag = next;
                }
                if (to == -1)
                {
                    lastFrag.End.Add(lastFrag.Start);
                    lastFrag.End.Add(termStart);
                }
                else
                {
                    for (int i = 0; i < to - from; i++)
                    {
                        var next = atomFrag.Clone();
                        frag.End.Add(next.Start);
                        frag.End.Add(termStart);
                        frag.End = next.End;
                    }
                    frag.End.Add(termStart);
                }
            }
      
            frag.End = termEnd;
            return frag;
        }
    }

    class AtomExp : AExp
    {
        public AlterExp group;
        public CharsetExp charset;
        public MetaExp meta;

        protected override bool ImplParse(StringReader stringReader)
        {
            char c = stringReader.Peek();
            if (c == '(')
            {
                stringReader.Read();
                string str = stringReader.ReadMatch('(', ')');
                if (str == null)
                {
                    return false;
                }
                group = new AlterExp();
                return group.Parse(new StringReader(str));
            }
            if (c == '[')
            {
                stringReader.Read();
                string str = stringReader.ReadMatch('[', ']');
                if (str == null)
                {
                    return false;
                }
                charset = new CharsetExp();
                return charset.Parse(new StringReader(str));
            }
            meta = new MetaExp();
            return meta.Parse(stringReader);
        }

        public override bool Match(StringReader input)
        {
            if (group != null)
            {
                return group.Match(input);
            }
            if (charset != null)
            {
                return charset.Match(input);
            }
            if (meta != null)
            {
                return meta.Match(input);
            }
            return false;
        }

        public override NFAFrag ToNFA()
        {
            if (group != null)
            {
                return group.ToNFA();
            }
            if (charset != null)
            {
                return charset.ToNFA();
            }
            if (meta != null)
            {
                return meta.ToNFA();
            }
            return null;
        }
    }

    class MetaExp : AExp
    {
        public MetaType metaType;
        public char char1;
        public char char2;
        public bool incharset = false;
        public CharFlag flag;//TODO 边界的解析和匹配（连续边界可压缩）

        protected override bool ImplParse(StringReader stringReader)
        {
            char c = stringReader.Read();
            if (incharset)
            {
                if (char.IsLetterOrDigit(c) && stringReader.Peek() == '-')
                {
                    metaType = MetaType.Range;
                    stringReader.Read();
                    char d = stringReader.Read();
                    if (char.IsLetter(c) && char.IsLetter(d) && c <= d)
                    {
                        char1 = c; char2 = d;
                        return true;
                    }
                    if (char.IsDigit(c) && char.IsDigit(d) && c <= d)
                    {
                        char1 = c; char2 = d;
                        return true;
                    }
                    return false;
                }

                if (c == '\\')
                {
                    char d = stringReader.Read();
                    if (d == 'd') { metaType = MetaType.Digit; return true; }
                    if (d == 'D') { metaType = MetaType.NDigit; return true; }
                    if (d == 'w') { metaType = MetaType.Word; return true; }
                    if (d == 'W') { metaType = MetaType.NWord; return true; }
                    if (d == 's') { metaType = MetaType.Space; return true; }
                    if (d == 'S') { metaType = MetaType.NSpace; return true; }
                    if (d == 't') { metaType = MetaType.Char; char1 = '\t'; return true; }
                    if (d == 'r') { metaType = MetaType.Char; char1 = '\r'; return true; }
                    if (d == 'n') { metaType = MetaType.Char; char1 = '\n'; return true; }
                    if (d == '\\' || d == '-')
                    {
                        metaType = MetaType.Char;
                        char1 = d;
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                if (c == '.') { metaType = MetaType.Any; return true; }
                if (c == '\\')
                {
                    char d = stringReader.Read();
                    if (d == 'd') { metaType = MetaType.Digit; return true; }
                    if (d == 'D') { metaType = MetaType.NDigit; return true; }
                    if (d == 'w') { metaType = MetaType.Word; return true; }
                    if (d == 'W') { metaType = MetaType.NWord; return true; }
                    if (d == 's') { metaType = MetaType.Space; return true; }
                    if (d == 'S') { metaType = MetaType.NSpace; return true; }
                    if (d == 't') { metaType = MetaType.Char; char1 = '\t'; return true; }
                    if (d == 'r') { metaType = MetaType.Char; char1 = '\r'; return true; }
                    if (d == 'n') { metaType = MetaType.Char; char1 = '\n'; return true; }
                    if ("\\.*+?{}[]()^$".Contains(d))
                    {
                        metaType = MetaType.Char;
                        char1 = d;
                        return true;
                    }

                    return false;
                }
            }

            metaType = MetaType.Char;
            char1 = c;
            return true;

        }

        public override bool Match(StringReader input)
        {
            char c = input.Read();
            switch (metaType)
            {
                case MetaType.Any:
                    return c != '\n' && c != '\r';
                case MetaType.Digit:
                    return char.IsDigit(c);
                case MetaType.NDigit:
                    return !char.IsDigit(c);
                case MetaType.Space:
                    return char.IsWhiteSpace(c);
                case MetaType.NSpace:
                    return !char.IsWhiteSpace(c);
                case MetaType.Word:
                    return char.IsLetterOrDigit(c) || c == '_';
                case MetaType.NWord:
                    return !char.IsLetterOrDigit(c) && c != '_';
                case MetaType.Range:
                    return c >= char1 && c <= char2;
                case MetaType.Char:
                    return c == char1;
                //case MetaType.Bound:
                //    {
                //        char back = input.ReadBack(2);
                //        char forward = input.Peek();
                //        if (c != char.MinValue) input.RollBack();
                //        bool b1 = back == char.MinValue || char.IsWhiteSpace(back);
                //        bool b2 = forward == char.MinValue || char.IsWhiteSpace(forward);
                //        if (b1 && !b2 || !b1 && b2)
                //        {
                //            return true;
                //        }
                //        return false;
                //    }
                //case MetaType.LineBegin:
                //    if (c != char.MinValue) input.RollBack();
                //    return input.Index == 0;
                //case MetaType.LineEnd:
                //    return c == char.MinValue;
                default:
                    return false;
            }
        }

        public override NFAFrag ToNFA()
        {
            NFAFrag frag = new NFAFrag();
            frag.Start.Exp = this;
            frag.Start.Add(frag.End);
            return frag;
        }

        public bool IsEqualExp(MetaExp rhs)
        {
            if(metaType == rhs.metaType && char1==rhs.char1 && char2==rhs.char2 && flag == rhs.flag)
            {
                return true;
            }
            return false;
        }
    }

    class CharsetExp : AExp
    {
        public bool Not;
        public List<MetaExp> metas;

        protected override bool ImplParse(StringReader stringReader)
        {
            int beginIdx = stringReader.Index;
            char c = stringReader.Peek();
            if (c == '^')
            {
                stringReader.Read();
                Not = true;
            }
            metas = new List<MetaExp>();
            while (!stringReader.EOF())
            {
                MetaExp meta = new MetaExp();
                meta.incharset = true;
                if (!meta.Parse(stringReader))
                {
                    return false;
                }
                metas.Add(meta);
            }
            return true;
        }

        public override bool Match(StringReader input)
        {
            bool match = false;
            foreach (MetaExp meta in metas)
            {
                if (meta.Match(input))
                {
                    match = true;
                    break;
                }
                else
                {
                    input.RollBack();
                }
            }
            if (!match)
            {
                input.Forward();
            }
            if (Not)
            {
                match = !match;
            }
            return match;
        }

        public override NFAFrag ToNFA()
        {
            NFAFrag frag = new NFAFrag();
            frag.Start.Exp = this;
            frag.Start.Add(frag.End);
            return frag;
        }


        public bool IsEqualExp(CharsetExp rhs)
        {
            if(Not == rhs.Not && metas.Count == rhs.metas.Count)
            {
                for(int i=0;i<metas.Count;i++)
                {
                    if (! metas[i].IsEqualExp(rhs.metas[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }


}
