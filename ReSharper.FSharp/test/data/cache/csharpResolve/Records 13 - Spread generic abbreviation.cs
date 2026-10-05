public class Class1
{
  public Class1()
  {
    Module.R r = new Module.R(1, "a");
    int x = r.X;
    Module.G<string> g = new Module.G<string>("s", 2);
    string gx = g.X;
    Module.R2 r2 = new Module.R2(1, true);
    int r2x = r2.X;
  }
}
