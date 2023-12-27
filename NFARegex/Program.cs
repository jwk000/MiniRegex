﻿using System.Diagnostics;

namespace NFARegex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int i = 1;
            {
                // Test Case 1: Basic Meta Characters
                NFARegex reg = new NFARegex("abc");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("abc")); // Expected: Match
            }
            {
                // Test Case 2: Meta Characters with Quantifiers
                NFARegex reg = new NFARegex("a+");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("aaa")); // Expected: Match
            }

            {
                // Test Case 3: Character Set
                NFARegex reg = new NFARegex("[aeiou]");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("o")); // Expected: Match
            }

            {
                // Test Case 4: Character Set with Quantifier
                NFARegex reg = new NFARegex("[aeiou]+");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("aei")); // Expected: Match
            }

            {
                // Test Case 5: Negated Character Set
                NFARegex reg = new NFARegex("[^aeiou]");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("x")); // Expected: Match
            }

            {
                // Test Case 6: Character Range
                NFARegex reg = new NFARegex("[a-z]");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("m")); // Expected: Match
            }

            {
                // Test Case 7: Quantifier Range
                NFARegex reg = new NFARegex("a{2,4}");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("aaa")); // Expected: Match
            }

            {
                // Test Case 8: Grouping
                NFARegex reg = new NFARegex("(abc)+");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("abcabc")); // Expected: Match
            }

            {
                // Test Case 9: Meta Characters and Escaping
                NFARegex reg = new NFARegex(@"\d\s\w");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("1 A")); // Expected: Match
            }

            {
                // Test Case 10: Complex Pattern
                NFARegex reg = new NFARegex(@"(\d{2,3}[a-z]){2}");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("12b45c")); // Expected: Match
            }

            {
                // Test Case 11: Alternation
                NFARegex reg = new NFARegex("cat|dog");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("dog")); // Expected: Match
            }

            {
                // Test Case 12: Escaping Special Characters
                NFARegex reg = new NFARegex(@"\[\d{3}\]");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("[123]")); // Expected: Match
            }

            {
                // Test Case 13: Multiple Quantifiers
                NFARegex reg = new NFARegex(@"\w{3,5}\d{2,3}");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("abc123")); // Expected: Match
            }

            {
                // Test Case 14: Grouping and Quantifiers
                NFARegex reg = new NFARegex(@"(ab)+\d{2}");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("abab12")); // Expected: Match
            }

            {
                // Test Case 15: Complex Combination
                NFARegex reg = new NFARegex(@"(\w{2}\d+)+[aeiou]+");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("ab12aeiou")); // Expected: Match
            }

            {
                // Test Case 16: Nested Grouping
                NFARegex reg = new NFARegex(@"(\d{2,3}[a-z]+){2,3}");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("12xyz34abc567defg")); // Expected: Match
            }

            {
                // Test Case 17: Start and End Anchors
                NFARegex reg = new NFARegex(@"^\d{3}-\w{2}$");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("123-AB")); // Expected: Match
            }

            {
                // Test Case 18: Word Boundary
                NFARegex reg = new NFARegex(@"\w+word\b");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("keyword")); // Expected: Match
            }


            {
                //19
                NFARegex reg = new NFARegex(@"[ \t\r\n]*\w+\s*=\s*[0-9]{1,}\s*");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("    aabb   =  9988 "));
            }

            {
                //20
                NFARegex reg = new NFARegex(@"(-\d{2,3}-\d{3})+");
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("-11-222-333-44")); //false
                //21
                Console.WriteLine("{0} {1}", i++, reg.IsMatch("-11-222-333-555")); //true
            }

            {
                //22
                NFARegex reg = new NFARegex(@"[a-f]*abc");
                bool ret = reg.IsMatch("abcdefabcdefabc");
                Console.WriteLine("{0} {1}", i++, ret);
            }

            {
                //23
                NFARegex reg = new NFARegex(@"a?a");
                bool ret = reg.IsMatch("a");
                Console.WriteLine("{0} {1} ", i++, ret);
            }

            {
                //24
                Stopwatch sw = new Stopwatch();
                sw.Start();
                NFARegex reg = new NFARegex(@"a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?aaaaaaaaaaaaaaaaaaaaaaaaa");
                bool ret = reg.IsMatch("aaaaaaaaaaaaaaaaaaaaaaaaa");//3s
                Console.WriteLine("{0} {1} {2}", i++, ret, sw.Elapsed);
                sw.Stop();
            }

            {
                //25
                Stopwatch sw = new Stopwatch();
                sw.Start();
                NFARegex reg = new NFARegex(@"a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*aaaaaaaaaaaaaa");
                bool ret = reg.IsMatch("aaaaaaaaaaaaaa");
                Console.WriteLine("{0} {1} {2}", i++, ret, sw.Elapsed);//10s
                sw.Stop();
            }

        }

    }
}