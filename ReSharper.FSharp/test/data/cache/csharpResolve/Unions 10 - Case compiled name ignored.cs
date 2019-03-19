using static Module;

public class Class1
{
  public Class1()
  {
    U a = U.CaseA;
    U b = U.NewCaseB(123);

    U aError = U.AName;
    U bError = U.NewBName(123);
  }
}
