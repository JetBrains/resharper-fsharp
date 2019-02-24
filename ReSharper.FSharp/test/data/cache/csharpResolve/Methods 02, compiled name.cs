using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    var t = new T();
    int m = t.InstanceMethod();
    int sm = T.StaticMethodCompiled();

    int oInt = t.OverloadInt(123, 123);
    string oString = t.OverloadString("123", "123");

    int oStaticInt = T.StaticOverloadInt(123, 123);
    string oStaticString = T.StaticOverloadString("123", "123");
  }
}
