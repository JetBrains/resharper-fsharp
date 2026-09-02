using System.Runtime.CompilerServices;

public class Named
{
  [IndexerName("Foo")]
  public int this[int i] => i;
}

public class Plain
{
  public int this[int i] => i;
}
