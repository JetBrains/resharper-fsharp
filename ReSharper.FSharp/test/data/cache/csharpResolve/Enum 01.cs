using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    E f1 = E.Field1;
    int f2 = (int) E.Field2;
    int f3 = (int) E.Field2Compiled;

    var _ = f1 switch
    {
      E.Field1 => true,
      E.Field2 => false
    };
  }
}
