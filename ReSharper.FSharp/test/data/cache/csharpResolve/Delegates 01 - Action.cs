using static Module;

public class Class1
{
  public static void M()
  {
  }

  public Class1()
  {
    D d1 = M;
    var r1 = d1();

    D d2 = f;
    var r2 = d2();
  }
}
