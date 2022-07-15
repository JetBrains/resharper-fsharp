using System;

[Obsolete("Class is obsolete")]
public class ObsoleteClass
{
}

[Obsolete("Struct is obsolete")]
public struct ObsoleteStruct
{
}

[Obsolete("Interface is obsolete")]
public interface IObsoleteInterface
{
    int P { get; }
}

[Obsolete("Enum is obsolete")]
public enum ObsoleteEnum
{
    A
}

[Obsolete("Delegate is obsolete")]
public delegate void ObsoleteDelegate();

public class Class
{
    public Class()
    {
    }

    [Obsolete("Constructor is obsolete")]
    public Class(int i)
    {
    }

    [Obsolete("Field is obsolete")] public static readonly int ObsoleteField = 1;

    [Obsolete("Method is obsolete")]
    public static void ObsoleteMethod()
    {
    }

    [Obsolete("Property is obsolete")] public static int ObsoleteProperty => 1;

    [Obsolete("Event is obsolete")] public static event EventHandler ObsoleteEvent;
}

public enum Enum
{
    [Obsolete("Enum field is obsolete")]
    A
}
