using System.Collections;
using System.Runtime.Serialization;
using static Module;

namespace ClassLibrary1
{
    public class Class1 : E1
    {
        public Class1()
        {
            var e1 = new E1(data0: 123);
            var e2 = new E2(named: 123);

            var e1error = new E1();
            var e2error = new E2();

            IDictionary data = e1.Data;
            int d = e1.Data0;
            int n = e2.Named;

            e1.Data0 = 123;
            e2.Named = 123;
        }

        protected Class1(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
