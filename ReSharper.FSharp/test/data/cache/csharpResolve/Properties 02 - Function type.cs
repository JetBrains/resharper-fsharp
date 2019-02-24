using Microsoft.FSharp.Core;
using static Module;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1()
        {
            FSharpFunc<int, FSharpFunc<int, int>> f = T.Fun;
        }
    }
}
