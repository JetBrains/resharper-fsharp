#nullable enable
public class Class
{
  public static T NotNull<T>(T p) => p;
  public static T? Nullable<T>(T p) where T : class => null;
}
