using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniRegex
{

    class NFAState
    {
        static int __id = 0;
        public int ID;
        public IExp Exp = null;
        public List<NFAState> Branchs = new List<NFAState>();

        public NFAState()
        {
            ID = __id++;
        }
        public override string ToString()
        {
            return $"{ID}|<{Exp}>|{Branchs.Count}";
        }

        public void Add(NFAState state)
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

        public bool IsEqualExp(NFAState state)
        {
            if(Exp==null && state.Exp == null)
            {
                return true;
            }

            if(Exp!=null && state.Exp != null)
            {
                if(Exp is MetaExp && state.Exp is MetaExp)
                {
                    if((Exp as MetaExp).IsEqualExp(state.Exp as MetaExp))
                    {
                        return true;
                    }
                }
                if(Exp is CharsetExp && state.Exp is CharsetExp)
                {
                    if((Exp as CharsetExp).IsEqualExp(state.Exp as CharsetExp))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    class NFAFrag
    {
        public NFAState Start = new NFAState();
        public NFAState End = new NFAState();
        public NFAFrag()
        {

        }

        public NFAFrag Clone()
        {
            NFAFrag ret = new NFAFrag();

            List<NFAState> list1 = new List<NFAState>();
            List<NFAState> list2 = new List<NFAState>();
            Dictionary<NFAState, NFAState> visited = new Dictionary<NFAState, NFAState>();
            list1.Add(Start);
            list2.Add(ret.Start);
            while (list1.Count > 0)
            {
                var state = list1[0];
                var copy = list2[0];
                list1.RemoveAt(0);
                list2.RemoveAt(0);
                visited.Add(state, copy);
                copy.Exp = state.Exp;

                foreach (var b in state.Branchs)
                {
                    bool isvisited = visited.ContainsKey(b);
                    NFAState c = null;
                    if (b == End)
                    {
                        c = ret.End;
                    }
                    else if (isvisited)
                    {
                        c = visited[b];
                    }
                    else
                    {
                        c = new NFAState() { Exp = b.Exp };
                    }
                    copy.Add(c);
                    if (!isvisited)
                    {
                        list1.Add(b);
                        list2.Add(c);
                    }
                }
            }

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("digraph G{");
            List<NFAState> list = new List<NFAState>();
            List<NFAState> visited = new List<NFAState>();
            list.Add(Start);
            sb.Append($"s{Start.ID}[label=\"start\"];");
            sb.Append($"s{End.ID}[label=\"end\"];");
            while (list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                if (visited.Contains(s))
                {
                    continue;
                }
                visited.Add(s);
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

        public void DumpGraph(string name)
        {
            if (!Directory.Exists("image"))
            {
                Directory.CreateDirectory("image");
            }
            File.WriteAllText($"{name}.dot", this.ToString());
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "dot";
            process.StartInfo.Arguments = string.Format($"{name}.dot -Tjpg -o image/{name}.jpg");
            process.Start();
            process.WaitForExit();
        }

        public void Merge()
        {
            Dictionary<NFAState, List<NFAState>> dict = new Dictionary<NFAState, List<NFAState>>();
            List<NFAState> list = new List<NFAState>();
            List<NFAState> visited = new List<NFAState>();
            list.Add(Start);
            while (list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                visited.Add(s);
                foreach (var b in s.Branchs)
                {
                    if (!dict.ContainsKey(b)) { dict[b] = new List<NFAState>(); }
                    dict[b].Add(s);
                    if (!visited.Contains(b))
                    {
                        list.Add(b);
                    }
                }
            }
            visited.Clear();
            list.Add(Start);
            while (list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                visited.Add(s);
                for (int i = s.Branchs.Count - 1; i >= 0; i--)
                {
                    var b = s.Branchs[i];
                    //如果b是空节点且只有一个前置节点s，s和b合并
                    if (b.Exp == null && dict[b].Count == 1)
                    {
                        s.Branchs.RemoveAt(i);
                        dict.Remove(b);
                        s.Branchs = s.Branchs.Union(b.Branchs).ToList();
                        list.Add(s);
                        if (b == End) { End = s; }
                        break;
                    }
                    if (!visited.Contains(b))
                    {
                        list.Add(b);
                    }
                }
            }
        }
    }

    class NFARegex
    {
        static int id = 0;
        NFAState Start, End;
        PatternExp Exp = new PatternExp();
        public NFARegex(string pattern)
        {
            id++;
            if (!Exp.Parse(new StringReader(pattern)))
            {
                throw new Exception("invalid pattern string");
            }
            NFAFrag frag = Exp.ToNFA();
            frag.DumpGraph($"regex{id}.raw");
            frag.Merge();
            frag.DumpGraph($"regex{id}");
            Start = frag.Start;
            End = frag.End;
        }

        public bool IsMatch(string input)
        {
            //openlist存放当前查找字符访问过的状态，不能重复访问，防止死循环
            List<NFAState> OpenList = new List<NFAState>();
            List<NFAState> CurList = new List<NFAState>();
            List<NFAState> NexList = new List<NFAState>();
            var sr = new StringReader(input);
            CurList.Add(Start);

            for (int index = 0; index < sr.Input.Length; index++)
            {
                foreach (var s in CurList)
                {
                    OpenList.Clear();
                    DFS(s, sr, index, OpenList, NexList);
                }
                if (NexList.Count == 0)
                {
                    return false;
                }
                CurList.Clear();
                var tmp = CurList;
                CurList = NexList;
                NexList = tmp;
            }

            foreach (var s in CurList)
            {
                if (CanMatchEnd(s))
                {
                    return true;
                }
            }

            return false;
        }

        void DFS(NFAState s, StringReader sr, int index, List<NFAState> open, List<NFAState> next)
        {
            foreach (var t in s.Branchs)
            {
                sr.SetIndex(index);
                if (t.Match(sr))
                {
                    if (t.Exp == null)
                    {
                        if (!open.Contains(t))
                        {
                            open.Add(t);
                            DFS(t, sr, index, open, next);
                        }
                    }
                    else
                    {
                        next.Add(t);
                    }
                }
            }
        }

        bool CanMatchEnd(NFAState s)
        {
            if (s == End) return true;
            if (End.Exp != null) return false;
            List<NFAState> Q = new List<NFAState>() { s };
            while(Q.Count > 0)
            {
                var h = Q[0];
                Q.RemoveAt(0);
                foreach (var t in h.Branchs)
                {
                    if (t == End)
                    {
                        return true;
                    }
                    if (t.Exp == null)
                    {
                        Q.Add(t);
                    }
                }

            }
            return false;
        }
    }

}
