using System;

namespace Etsu.Tests
{
    internal static class TestSupport
    {
        public static int Failures;

        public static void Check(bool condition, string name)
        {
            if (condition)
            {
                Console.WriteLine("PASS " + name);
            }
            else
            {
                Console.WriteLine("FAIL " + name);
                Failures++;
            }
        }
    }
}
