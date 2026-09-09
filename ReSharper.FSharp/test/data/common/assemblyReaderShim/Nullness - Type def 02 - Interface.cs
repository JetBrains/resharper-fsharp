#nullable enable
public interface IBase<T>
{
  T Value { get; }
}

public class WithoutNull : IBase<string>
{
  public string Value => "";
}

public class WithNull : IBase<string?>
{
  public string? Value => null;
}
