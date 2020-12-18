public class Class1
{
  public Class1()
  {
    var t = new T();

    var _ = t[0];
    var __ = t.get_Item(0);
    var ___ = T.get_Item(0);
    t[0] = 0;
    t.set_Item(0, 0);
    T.set_Item(0, 0);
  }
}
