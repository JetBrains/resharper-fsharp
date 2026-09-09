#nullable enable
using System;

public class Args : EventArgs { }

public class Class
{
  public static event EventHandler<Args> NotNull = null!;
  public static event EventHandler<Args?> Nullable = null!;
}
