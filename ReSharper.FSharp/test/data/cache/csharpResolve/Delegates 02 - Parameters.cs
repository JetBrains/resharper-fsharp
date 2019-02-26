using static Module;

public class Class1
{
  public static void M(int i, double d)
  {
  }

  public Class1()
  {
    D d1 = M;
    var r1 = d1(1, 1.0);

    D d2 = f;
    var r2 = d2(1, 1.0);
  }
}
