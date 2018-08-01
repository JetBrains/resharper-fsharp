using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U? uError = U.NewCase(item: 123);
            SU? su = SU.NewCase(item: 123);

            SU u = su.Value;

            int t = u.Tag;
            int i = u.Item;

            bool isCaseError = u.IsCase;
            int tagsError = U.Tags.CaseA;
            U.Case c = (U.Case) u;
        }
    }
}
