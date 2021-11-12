public enum Enum
{
  A = 0,
  B = 1,
  C = A | B,
  D = 2
}

public enum EnumShort : short
{
  A,
  B
}

public enum EmptyEnum
{
}

public class Class
{
}

public enum EnumWrongBaseType : Foo
{
  A,
  B
}

public enum EnumWrongValueType
{
  A = ""
}

public enum EnumOverflowValue : ushort
{
  A = -1,
  B = 0
}

public enum EnumSameFields
{
  A,
  A
}
