using System.Collections.Generic;
using System.Runtime.Serialization;
using static Module;

namespace ClassLibrary1
{
    public class Class1 : E
    {
        public Class1()
        {
            var e = new E();
            var o = new object();
            var c = EqualityComparer<E>.Default;

            string s = e.ToString();
            string m = e.Message;

            int hc1 = e.GetHashCode();
            int hc2 = e.GetHashCode(c);

            bool b1 = e.Equals(e);
            bool b2 = e.Equals(o);
            bool b3 = e.Equals(o, c);
        }

        protected Class1(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
