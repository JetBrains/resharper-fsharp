#nullable enable
public class Class
{
  public static void Out(out string? p) { p = null; }
  public static void OutNotNull(out string p) { p = ""; }
  public static void Ref(ref string p) { }
  public static void In(in string? p) { }
}
