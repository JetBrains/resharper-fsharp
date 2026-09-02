public unsafe class Class
{
  public delegate*<int, int> Field;

  public delegate*<int, int> Property { get; set; }

  public static void M(delegate*<int, int> f) { }

  public static delegate*<int, int> N() => null;
}
