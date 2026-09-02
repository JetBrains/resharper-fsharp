public unsafe class SingleIndexer
{
  public int this[delegate*<int, int> f] => 1;
}

public unsafe class PointerAndString
{
  public int this[delegate*<int, int> f] => 1;
  public int this[string s] => 2;
}

public unsafe class ArgTypes
{
  public int this[delegate*<int, int> f] => 1;
  public int this[delegate*<string, string> f] => 2;
}

public unsafe class ByrefArg
{
  public int this[delegate*<int, int> f] => 1;
  public int this[delegate*<ref int, int> f] => 2;
}

public unsafe class ByrefReturn
{
  public int this[delegate*<int, int> f] => 1;
  public int this[delegate*<int, ref int> f] => 2;
}
