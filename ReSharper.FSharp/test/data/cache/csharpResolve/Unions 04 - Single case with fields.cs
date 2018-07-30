using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            U u = U.NewCase(item: 123);

            int t = u.Tag;
            int i = u.Item;

            bool isCaseError = u.IsCase;
            int tagsError = U.Tags.CaseA;
            U.Case c = (U.Case) u;
        }
    }
}
