using Microsoft.FSharp.Core;
using static Module;

public class Class1
{
  public Class1()
  {
    FSharpFunc<int, FSharpFunc<int, int>> f = T.Fun;
  }
}
