using System.Text.RegularExpressions;

namespace fake_fastgithub
{
    sealed class DomainPattern : IComparable<DomainPattern>
    {
        private readonly Regex regex;
        private readonly string domainPattern;

        public DomainPattern(string domainPattern)
        {
            this.domainPattern = domainPattern;
            var regexPattern = Regex.Escape(domainPattern).Replace(@"\*", @"[^\.]*");
            regex = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        private static int Compare(string x, string y)
        {
            var valueX = x.Replace('*', char.MaxValue);
            var valueY = y.Replace('*', char.MaxValue);
            return valueX.CompareTo(valueY);
        }

        public int CompareTo(DomainPattern? other)
        {
            if (other is null)
            {
                return 1;
            }

            var segmentsX = this.domainPattern.Split('.');
            var segmentsY = other.domainPattern.Split('.');
            var value = segmentsX.Length - segmentsY.Length;
            if (value != 0)
            {
                return value;
            }

            for (var i = segmentsX.Length - 1; i >= 0; i--)
            {
                var x = segmentsX[i];
                var y = segmentsY[i];

                value = Compare(x, y);
                if (value == 0)
                {
                    continue;
                }
                return value;
            }

            return 0;
        }

        public bool IsMatch(string domain)
        {
            return this.regex.IsMatch(domain);
        }

        public override string ToString()
        {
            return this.domainPattern;
        }
    }
}
