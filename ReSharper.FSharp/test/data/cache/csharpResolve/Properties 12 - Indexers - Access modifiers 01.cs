public class Class1
{
  public Class1()
  {
    var t = new T();

    var _ = t[0];
    t[0] = 0;

    var __ = t[""];
    t[""] = 0;
  }
}
