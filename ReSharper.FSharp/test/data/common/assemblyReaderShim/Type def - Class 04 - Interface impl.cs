using System;

public class Class1 : IDisposable
{
    public void Dispose()
    {
    }
}

public class Class2 : IDisposable
{
    void IDisposable.Dispose()
    {
    }
}

public class Class3 : IDisposable
{
}
