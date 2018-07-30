using System.Collections.Generic;
using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U a = U.CaseA;

            var o = new object();
            var c = EqualityComparer<U>.Default;

            string s = a.ToString();

            int hc1 = a.GetHashCode();
            int hc2 = a.GetHashCode(c);

            bool b1 = a.Equals(a);
            bool b2 = a.Equals(o);
            bool b3 = a.Equals(o, c);

            int ct1 = a.CompareTo(a);
            int ct2 = a.CompareTo(o);
            int ct3 = a.CompareTo(o, Comparer<U>.Default);
        }
    }
}
