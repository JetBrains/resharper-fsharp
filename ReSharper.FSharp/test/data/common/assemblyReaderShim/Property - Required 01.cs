using System.Diagnostics.CodeAnalysis;

public class Class
{
  public required int Req { get; set; }
  public int Opt { get; set; }
}

public class WithCtor
{
  public required int Req { get; set; }

  [SetsRequiredMembers]
  public WithCtor(int r) { Req = r; }
}
