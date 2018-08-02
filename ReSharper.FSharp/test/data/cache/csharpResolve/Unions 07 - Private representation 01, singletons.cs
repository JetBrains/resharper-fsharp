using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U a = U.CaseA;
            U b = U.CaseB;

            bool isA = a.IsCaseA;
            bool isB = a.IsCaseB;

            int t = a.Tag;
            int tA = U.Tags.CaseA;

            int m = U.Prop;
        }
    }
}
