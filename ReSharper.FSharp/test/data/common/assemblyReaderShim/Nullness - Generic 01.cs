#nullable enable
using System.Collections.Generic;

public class Class
{
  public static List<string> NotNull() => null!;
  public static List<string?> NullableItem() => null!;
  public static List<string>? NullableList() => null!;
  public static Dictionary<string, string?> Pairs() => null!;
  public static List<List<string?>> Nested() => null!;
}
