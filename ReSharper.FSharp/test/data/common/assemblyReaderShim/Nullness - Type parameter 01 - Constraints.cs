#nullable enable
public class Class
{
  public static void NotNullConstraint<T>(T p) where T : notnull { }
  public static void ClassConstraint<T>(T p) where T : class { }
  public static void NullableClassConstraint<T>(T p) where T : class? { }
  public static void NoConstraint<T>(T p) { }
}
