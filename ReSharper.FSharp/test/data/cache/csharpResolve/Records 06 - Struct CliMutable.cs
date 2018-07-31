using System.Collections.Generic;
using static Module;

namespace ClassLibrary1
{
  public class Class1
  {
    public Class1()
    {
      SR r = new SR(field: 123);

      r.Field = 123;
    }
  }
}
