using System;

public enum Enum1 {}
public class Exception1 : Exception {}

public class Disposable : IDisposable
{
    public void Dispose() => throw new NotImplementedException();
}

public class Program
{
    public static void Main(string[] args)
    {
        new UnmanagedConstraint<int>();
        new UnmanagedConstraint<string>();
    }
}
