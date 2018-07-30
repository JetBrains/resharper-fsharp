using System.Collections.Generic;
using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            R r = new R(foo: 123, bar: 123.0);

            int foo = r.Foo;
            double bar = r.Bar;

            r.Foo = 123;
            r.Bar = 123;

            var o = new object();
            var c = EqualityComparer<R>.Default;

            string s = r.ToString();

            int hc1 = r.GetHashCode();
            int hc2 = r.GetHashCode(c);

            bool b1 = r.Equals(r);
            bool b2 = r.Equals(o);
            bool b3 = r.Equals(o, c);

            int ct1 = r.CompareTo(r);
            int ct2 = r.CompareTo(o);
            int ct3 = r.CompareTo(o, Comparer<R>.Default);

            var rError = new R();
        }
    }
}
