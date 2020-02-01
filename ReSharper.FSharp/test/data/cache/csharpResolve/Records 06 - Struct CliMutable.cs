using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    SR r1 = new SR(field: 123);
    r1.Field = 123;

    SR r2 = new SR();
    r2.Field = 123;
    
    M(r1);
  }

  private void M<T>(T t) where T : new()
  {
  }
}
