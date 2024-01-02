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

        //合并空转换，如果s可以经过空转换到达t，则s直接转换到t
        public void MergeEmpty()
        {
            Dictionary<NFAState, List<NFAState>> map = new Dictionary<NFAState, List<NFAState>>();
            List<NFAState> list = new List<NFAState>();
            List<NFAState> visited = new List<NFAState>();
            list.Add(Start);
            while (list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                if (visited.Contains(s))
                {
                    continue;
                }
                visited.Add(s);
                var next = MergeState(s);
                map.Add(s, next);
                foreach(var t in next)
                {
                    if (!visited.Contains(t))
                    {
                        list.Add(t);
                    }
                }
            }

            list.Clear();
            visited.Clear();
            list.Add(Start);
            while(list.Count > 0)
            {
                var s = list[0];
                list.RemoveAt(0);
                visited.Add(s);
                s.Branchs.Clear();
                s.Branchs.AddRange(map[s]);
                foreach(var t in s.Branchs)
                {
                    if (!visited.Contains(t))
                    {
                        list.Add(t);
                    }
                }
            }
        }

        List<NFAState> MergeState(NFAState state)
        {
            List<NFAState> ans = new List<NFAState>();
            List<NFAState> queue = new List<NFAState>();
            List<NFAState> visited = new List<NFAState>();
            queue.Add(state);
            while (queue.Count > 0)
            {
                var s = queue[0];
                queue.RemoveAt(0);
                if (visited.Contains(s))
                {
                    continue;
                }
                visited.Add(s);
                foreach(var t in s.Branchs)
                {
                    if(t.Exp == null)
                    {
                        if (t == End) ans.Add(t);
                        if (!visited.Contains(t))
                        {
                            queue.Add(t);
                        }
                    }
                    else
                    {
                        ans.Add(t);
                    }
                }
            }
            return ans;
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
            //DumpNFAGraph(frag, $"regex{id}.0");
            frag.MergeEmpty();
            //DumpNFAGraph(frag, $"regex{id}.1");
            Start = frag.Start;
            End = frag.End;
        }

        public string NFAToString(NFAState start, NFAState end)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("digraph G{");
            List<NFAState> list = new List<NFAState>();
            List<NFAState> visited = new List<NFAState>();
            list.Add(start);
            sb.Append($"s{start.ID}[label=\"start\"];");
            sb.Append($"s{end.ID}[label=\"end\"];");
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

        public void DumpNFAGraph(NFAFrag frag, string name)
        {
            Directory.CreateDirectory("dump");
            File.WriteAllText($"dump/{name}.dot", NFAToString(frag.Start,frag.End));
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "dot";
            process.StartInfo.Arguments = string.Format($"dump/{name}.dot -Tjpg -o dump/{name}.jpg");
            process.Start();
            process.WaitForExit();
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
            while (Q.Count > 0)
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
