public struct ManagedStruct
{
  public string Name;
}

public class Class
{
  public static void Unmanaged<T>(T x) where T : unmanaged { }
  public static void Struct<T>(T x) where T : struct { }
  public static void New<T>(T x) where T : new() { }
  public static void Plain<T>(T x) { }
}
