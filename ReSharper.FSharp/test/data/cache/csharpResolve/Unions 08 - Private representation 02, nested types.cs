using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U a = U.NewCaseA(item: 123);
            U b = U.NewCaseB(named: 123);
            U c = U.NewCaseC(item1: 123, other: 123.0);

            U.CaseA ca = (U.CaseA) a;
            U.CaseB cb = (U.CaseB) b;
            U.CaseC cc = (U.CaseC) c;

            int aItem = ca.Item;
            int bNamed = cb.Named;
            int cItem0 = cc.Item1;
            double cOther = cc.Other;

            int aItemError = a.Item;
            int bNamedError = b.Named;
            int cItem1Error = c.Item1;
            double cOtherError = c.Other;

            bool isA = a.IsCaseA;
            bool isB = a.IsCaseB;
            bool isC = a.IsCaseC;

            int tA = U.Tags.CaseA;
            int tB = U.Tags.CaseB;
            int tC = U.Tags.CaseC;

            int t = a.Tag;

            int m = U.Prop;
        }
    }
}
