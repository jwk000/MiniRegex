using System.Diagnostics;

namespace MiniRegex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var testcases = new[]
            {
                new []{ "abc","abc" },
                new []{ "a+","aaa" },
                new []{ "[aeiou]" ,"o"},
                new []{ "[0-8]+", "012345678" },
                new []{ "[^aeiou]" ,"x"},
                new []{ "[a-z]" ,"m"},
                new []{ "a{2,4}" ,"aaa"}, //7
                new []{ "(abc)+" ,"abcabc"},
                new []{ "cat|dog" ,"dog"},
                new []{ @"\d\s\w" ,"1 a"},
                new []{ @"(\d{2,3}[a-z]){2}" , "12b45c" },
                new []{ @"\[\d{3}\]" ,"[123]"},
                new []{ @"\w{3,5}\d{2,3}" ,"abc123"},
                new []{ @"(ab)+\d{2}" , "ab12aeiou" },
                new []{ @"(\d{2,3}[a-z]+){2,3}", "12xyz34abc567defg" },
                new []{ @"[ \t\r\n]*\w+\s*=\s*[0-9]{1,}\s*" , "    aabb   =  9988 " },
                new []{ @"(-\d{2,3}-\d{3})+" , "-11-222-333-555" },
                new []{ @"[a-f]*abc" , "abcdefabcdefabc" },
                new []{ @"a?a?aa" ,"aa"},
                new []{ @"a*a*aa" ,"aa"},
                new []{ @"a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?aaaaaaaaaaaaaaaaaaaaaaaaa" , "aaaaaaaaaaaaaaaaaaaaaaaaa" },
                new []{ @"a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*aaaaaaaaaaaaaa" , "aaaaaaaaaaaaaa" }

            };

            Stopwatch sw = new Stopwatch();
            //foreach (var tc in testcases)
            //{
            //    sw.Restart();
            //    MiniRegex reg = new MiniRegex(tc[0]);
            //    bool ret = reg.IsMatch(tc[1]);
            //    sw.Stop();
            //    Console.WriteLine("{0} --- {1}\n is match:{2} use:{3}ms\n", tc[0], tc[1],ret,sw.ElapsedMilliseconds);
            //}

            //foreach (var tc in testcases)
            //{
            //    sw.Restart();
            //    NFARegex reg = new NFARegex(tc[0]);
            //    bool ret = reg.IsMatch(tc[1]);
            //    sw.Stop();
            //    Console.WriteLine("{0} --- {1}\n is match:{2} use:{3}ms\n", tc[0], tc[1], ret, sw.ElapsedMilliseconds);
            //}

            foreach (var tc in testcases)
            {
                sw.Restart();
                DFARegex reg = new DFARegex(tc[0]);
                bool ret = reg.IsMatch(tc[1]);
                sw.Stop();
                Console.WriteLine("{0} --- {1}\n is match:{2} use:{3}ms\n", tc[0], tc[1], ret, sw.ElapsedMilliseconds);
            }
        }

    }
}