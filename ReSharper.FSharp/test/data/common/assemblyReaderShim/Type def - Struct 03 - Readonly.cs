public readonly struct RoStruct
{
  private readonly int myValue;
  public RoStruct(int value) { myValue = value; }
  public int Get() { return myValue; }
}

public struct MutStruct
{
  private int myValue;
  public MutStruct(int value) { myValue = value; }
  public int Get() { return myValue; }
  public readonly int GetRo() { return myValue; }
}
