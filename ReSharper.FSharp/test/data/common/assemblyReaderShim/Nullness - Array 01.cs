#nullable enable
public class Class
{
  public static string[] NotNull() => null!;
  public static string?[] NullableElement() => null!;
  public static string[]? NullableArray() => null!;
  public static string?[]? NullableBoth() => null!;
  public static string?[][] Jagged() => null!;
  public static string?[,] Rank2() => null!;
}
