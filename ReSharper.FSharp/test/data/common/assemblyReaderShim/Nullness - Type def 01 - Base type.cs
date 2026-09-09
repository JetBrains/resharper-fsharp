#nullable enable
public class Base<T>
{
  public T Value = default!;
}

public class WithoutNull : Base<string> { }

public class WithNull : Base<string?> { }
