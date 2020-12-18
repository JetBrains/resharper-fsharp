public class Class1
{
  public Class1()
  {
    var t = new T();

    var _ = t[0];
    t[0] = 0;
    var __ = t[""];
    t[""] = 0;
    
    var ___ = t.get_Item(0);
    var ____ = t.get_Item("");
    var _____ = t.Item;
    t.set_A(0, 0);
    t.set_A("", 0);
  }
}
