using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            R r = new R(123);
            r.VoidMethod();
            int r1 = r.Prop;
            int r2 = r.Method();
            int r3 = r.Method(123);
            int r4 = R.StaticMethod();
            int r5 = R.StaticProp;

            U c = U.Case;
            c.VoidMethod();
            int c1 = c.Prop;
            int c2 = c.Method();
            int c3 = c.Method(123);
            int c4 = U.StaticMethod();
            int c5 = U.StaticProp;

            E e = new E();
            e.VoidMethod();
            int e1 = e.Prop;
            int e2 = e.Method();
            int e3 = e.Method(123);
            int e4 = E.StaticMethod();
            int e5 = E.StaticProp;
        }
    }
}
