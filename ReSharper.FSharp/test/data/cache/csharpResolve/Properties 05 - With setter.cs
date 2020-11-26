public class Class1
{
  public Class1()
  {
    var t = new Module.T {P = 123};
    int p = t.P;
    t.P = 1;
  }
}
