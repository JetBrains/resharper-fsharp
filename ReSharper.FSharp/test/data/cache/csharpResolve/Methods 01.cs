using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    var t = new T();
    int m = t.Method();
    int sm = T.StaticMethod();

    int oInt = t.Overloads(123, 123);
    string oString = t.Overloads("123", "123");

    int oStaticInt = T.StaticOverloads(123, 123);
    string oStaticString = T.StaticOverloads("123", "123");
  }
}
