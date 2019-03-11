using System.Collections.Generic;
using static Module;

public class Class<T> where T : struct
{
  public Class()
  {
    R r = new R(field: 123);
    SR sr = new SR(field: 123);

    // Field is internal, and only getter is produced for property.
    r.Field = 123;
    sr.Field = 123;

    var o = new object();
    var c = EqualityComparer<SR>.Default;

    string s = sr.ToString();

    int hc1 = sr.GetHashCode();
    int hc2 = sr.GetHashCode(c);

    bool b1 = sr.Equals(sr);
    bool b2 = sr.Equals(o);
    bool b3 = sr.Equals(o, c);

    int ct1 = sr.CompareTo(sr);
    int ct2 = sr.CompareTo(o);
    int ct3 = sr.CompareTo(o, Comparer<SR>.Default);
  }
}

public class ClassR : Class<R>
{
}

public class ClassSR : Class<SR>
{
}
