#nullable enable
using System.Collections.Generic;

public class Class
{
  public static int? NullableInt() => null;
  public static List<int> Ints() => null!;
  public static KeyValuePair<string?, int> Pair() => default;
  public static KeyValuePair<int, string?>? NullablePair() => null;
}
