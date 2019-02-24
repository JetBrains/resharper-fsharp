using System;
using System.Collections.Generic;
using static Module;

public class Class1
{
  public Class1()
  {
    var t = new T<int>();
    Tuple<int, double> m = t.Method<double>();
  }
}
