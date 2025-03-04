using System.Numerics;

namespace CpuMetricTestService
{
    public static class Fibonacci
    {
        public static BigInteger Calculate(int n)
        {
            if (n == 0)
            {
                return 0;
            }
            if (n == 1)
            {
                return 1;
            }
            return Calculate(n - 1) + Calculate(n - 2);
        }
    }
}
