public class Class
{
  public Class()
  {
    var t = new Top.Nested();
    Top.f(t);

    int x = Top.NestedModule.x;
  }
}
