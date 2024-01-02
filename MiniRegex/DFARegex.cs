using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MiniRegex
{
    class DFAState
    {
        public bool IsEnd = false;
        static int __id = 0;
        public int ID;
        public IExp Exp = null;
        public List<DFAState> Branchs = new List<DFAState>();

        public DFAState()
        {
            ID = __id++;
        }
        public override string ToString()
        {
            return $"{ID}|<{Exp}>|{Branchs.Count}";
        }

        public void Add(DFAState state)
        {
            if (Branchs.Contains(state))
            {
                return;
            }
            Branchs.Add(state);
        }

        public bool Match(StringReader sr)
        {
            if (Exp == null)
            {
                return true;
            }
            return Exp.Match(sr);
        }

    }
    class DFARegex
    {
        static int id = 0;
        PatternExp Exp = new PatternExp();
        DFAState DFA = new DFAState();

        public DFARegex(string pattern)
        {
            id++;
            if (!Exp.Parse(new StringReader(pattern)))
            {
                throw new Exception("invalid pattern string");
            }
            NFAFrag frag = Exp.ToNFA();
            //frag.MergeEmpty();
            DFA = NFAToDFA(frag.Start, frag.End);
            DumpDFAGraph(DFA, $"regex{id}.2");
        }

        string DFAToString(DFAState start)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("digraph G{");
            List<DFAState> list = new List<DFAState>();
            List<DFAState> visited = new List<DFAState>();
            list.Add(start);
            sb.Append($"s{start.ID}[label=\"start\"];");
           
            while (list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                if (visited.Contains(s))
                {
                    continue;
                }
                visited.Add(s);
                if (s.IsEnd) { sb.Append($"s{s.ID}[label=\"end\"];"); }
                foreach (var b in s.Branchs)
                {
                    sb.Append($"s{s.ID} -> s{b.ID}");
                    if (b.Exp != null)
                    {
                        sb.Append($"[label=\"{b.Exp}\"]");
                    }
                    sb.Append(";");
                    if (!visited.Contains(b))
                    {
                        list.Add(b);
                    }
                }
            }
            sb.Append("}");

            return sb.ToString();

        }

        void DumpDFAGraph(DFAState start, string name)
        {
            Directory.CreateDirectory("dfa");
            File.WriteAllText($"dfa/{name}.dot", DFAToString(start));
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "dot";
            process.StartInfo.Arguments = string.Format($"dfa/{name}.dot -Tjpg -o dfa/{name}.jpg");
            process.Start();
            process.WaitForExit();
        }

        //合并所有多余的状态，也就是转DFA
        public DFAState NFAToDFA(NFAState Start, NFAState End)
        {
            List<int> queue = new List<int>();
            Dictionary<int, List<NFAState>> VirtualStates = new Dictionary<int, List<NFAState>>();
            Dictionary<int, DFAState> DFAStates = new Dictionary<int, DFAState>();
            List<int> visited = new List<int>();

            VirtualStates.Add(0, new List<NFAState>() { Start });
            DFAStates.Add(0, new DFAState() { Exp = Start.Exp });
            queue.Add(0);
            while (queue.Count > 0)
            {
                int key = queue[0];
                queue.RemoveAt(0);
                if (visited.Contains(key))
                {
                    continue;
                }
                visited.Add(key);
                //virtualstate1 -- pattern -- virtualstate2
                Dictionary<string, List<NFAState>> tostates = new Dictionary<string, List<NFAState>>();
                foreach (var s in VirtualStates[key])
                {
                    FindDFAStates(s, End, tostates);
                }

                foreach (var ts in tostates.Values)
                {
                    int hash = CalcHash(ts);
                    if (!DFAStates.ContainsKey(hash))
                    {
                        DFAStates[hash] = new DFAState() { Exp = ts[0].Exp, IsEnd = ts.Contains(End) };
                        VirtualStates[hash] = ts;
                    }
                    DFAStates[key].Add(DFAStates[hash]);
                    if (!visited.Contains(hash))
                    {
                        queue.Add(hash);
                    }
                }

            }
            return DFAStates[0];
        }

        //NFA合并后的状态集合可以算个int作为hash
        int CalcHash(List<NFAState> states)
        {
            List<int> ans = new List<int>();
            foreach (NFAState s in states)
            {
                ans.Add(s.ID);
            }
            ans.Sort();
            int ret = 0;
            foreach (int n in ans)
            {
                ret = ret * 131 + n;
            }
            return ret;
        }

        //找出st的所有后续字符集
        void FindDFAStates(NFAState st, NFAState end, Dictionary<string, List<NFAState>> states)
        {
            List<NFAState> queue = new List<NFAState>() { st };
            List<NFAState> visited = new List<NFAState>() { st };
            while (queue.Count > 0)
            {
                var s = queue[0];
                queue.RemoveAt(0);
                foreach (var t in s.Branchs)
                {
                    if (t.Exp == null && t!=end)
                    {

                        if (!visited.Contains(t))
                        {
                            visited.Add(t);
                            queue.Add(t);
                        }
                    }
                    else
                    {
                        string key;
                        if (t == end)
                        {
                            key = "end";
                        }
                        else
                        {
                            key = t.Exp.PatternString;
                        }
                        if (states.ContainsKey(key))
                        {
                            if (!states[key].Contains(t))
                            {
                                states[key].Add(t);
                            }
                        }
                        else
                        {
                            states.Add(key, new List<NFAState> { t });
                        }
                    }
                }
            }
        }

        
        public bool IsMatch(string input)
        {
            var sr = new StringReader(input);
            //由于state之间可能有交集，比如\w和\d，\d和[0-7]等，所以要多路查找
            List<DFAState> curlist = new List<DFAState>() { DFA };
            List<DFAState> nexlist = new List<DFAState>();
            for (int index = 0; index < sr.Input.Length; index++)
            {
                foreach(var s in curlist)
                {
                    foreach(var t in s.Branchs)
                    {
                        sr.SetIndex(index);
                        if (!t.IsEnd && t.Match(sr))
                        {
                            nexlist.Add(t);
                        }
                    }
                }
                if (nexlist.Count == 0)
                {
                    return false;
                }
                var tmp = curlist;
                curlist = nexlist;
                nexlist = tmp;
                nexlist.Clear();
            }
            
            foreach(var s in curlist)
            {
                if (s.IsEnd || s.Branchs.Exists(b => b.IsEnd))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
