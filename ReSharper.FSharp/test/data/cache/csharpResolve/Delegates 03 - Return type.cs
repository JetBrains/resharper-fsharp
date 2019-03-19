using static Module;

public class Class1
{
  public static double M(int a) => (double) a;

  public Class1()
  {
    D d1 = M;
    double r1 = d1(1);

    D d2 = f;
    double r2 = d2(1);
  }
}
