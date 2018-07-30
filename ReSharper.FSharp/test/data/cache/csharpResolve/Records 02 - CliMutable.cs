using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            R r = new R(foo: 123, bar: 123.0);
            R r2 = new R();

            int foo = r.Foo;
            double bar = r.Bar;

            r.Foo = 123;
            r.Bar = 123.0;
        }
    }
}
