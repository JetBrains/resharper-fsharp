using Microsoft.FSharp.Core;

public class Class1
{
  public Class1()
  {
    var none = FSharpOption<int>.None;
    var someInt = FSharpOption<int>.Some(1);
    var someTag = FSharpOption<int>.Tags.Some;
  }
}
