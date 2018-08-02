using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U? sa = U.NewCaseA(item: 123);
            U? sb = U.NewCaseB(named: 123);
            U? sc = U.NewCaseC(item1: 123, other: 123.0);

            U a = sa.Value;
            U b = sb.Value;
            U c = sc.Value;

            U.CaseA caError = (U.CaseA) a;
            U.CaseB cbError = (U.CaseB) b;
            U.CaseC ccError = (U.CaseC) c;

            int aItem = a.Item;
            int bItem = b.Item;
            int cItem = c.Item;
            int bNamed = b.Named;
            int cItem0 = c.Item1;
            double cOther = c.Other;

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
