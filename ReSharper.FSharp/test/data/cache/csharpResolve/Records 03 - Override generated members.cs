using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    var r = new R(foo: 123, bar: 234);
    string s = r.ToString();
    int c = r.GetHashCode();
    bool e = r.Equals((object) null);
  }
}
