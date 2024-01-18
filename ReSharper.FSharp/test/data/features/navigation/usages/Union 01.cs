using static Module;

public class Class1
{
  public Class1()
  {
    U a = U.A;
    U.B b = (U.B)U.NewB(123);

    var isA = a.IsA;
    var isB = b.IsB;

    var aTag = U.Tags.A;
    var bTag = U.Tags.B;
  }
}
