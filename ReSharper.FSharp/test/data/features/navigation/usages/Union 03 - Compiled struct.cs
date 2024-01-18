using Microsoft.FSharp.Core;

public class Class1
{
  public Class1()
  {
    var none = FSharpValueOption<int>.None;
    var someInt = FSharpValueOption<int>.Some(1);
    var isSome = someInt.IsSome;
  }
}
