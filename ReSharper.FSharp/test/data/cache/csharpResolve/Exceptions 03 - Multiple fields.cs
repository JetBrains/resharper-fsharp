using System.Collections;
using System.Runtime.Serialization;
using static Module;

namespace ClassLibrary1
{
    public class Class1 : E1
    {
        public Class1()
        {
            var e1 = new E1(data0: 123, data1: 123);
            var e2 = new E2(named: 123, other: 123.0);

            var e1error = new E1();
            var e2error = new E2();

            IDictionary data = e1.Data;
            int d0 = e1.Data0;
            double d1 = e1.Data1;

            int n = e2.Named;
            double o = e2.Other;

            e1.Data0 = 123;
            e1.Data1 = 123;
            e2.Named = 123;
            e2.Other = 123;
        }

        protected Class1(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
