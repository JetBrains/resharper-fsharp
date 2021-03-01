public class Class1
{
  public Class1()
  {
    using var t = new Module.T();
    t.Dispose();
    ((System.IDisposable) t).Dispose();
  }
}
