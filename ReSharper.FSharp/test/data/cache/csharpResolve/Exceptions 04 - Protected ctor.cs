using System.Runtime.Serialization;
using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            var e = new E();

            SerializationInfo info = null;
            StreamingContext context = new StreamingContext();
            var eErros = new E(info, context);
        }
    }
}