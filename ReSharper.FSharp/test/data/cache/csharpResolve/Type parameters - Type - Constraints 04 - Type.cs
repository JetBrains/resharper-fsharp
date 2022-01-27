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
        new BaseClassConstraint1<string>();
        new BaseClassConstraint1<String>();
        new BaseClassConstraint1<int>();
        new BaseClassConstraint1<Nullable<int>>();
        new BaseClassConstraint1<int?>();
        new BaseClassConstraint1<Enum>();
        new BaseClassConstraint1<Enum1>();

        new BaseClassConstraint2<Exception>();
        new BaseClassConstraint2<Exception1>();
        new BaseClassConstraint2<string>();
        new BaseClassConstraint2<String>();
        new BaseClassConstraint2<int>();
        new BaseClassConstraint2<Nullable<int>>();
        new BaseClassConstraint2<int?>();
        new BaseClassConstraint2<IDisposable>();

        new InterfaceConstraint<IDisposable>();
        new InterfaceConstraint<string>();
        new InterfaceConstraint<String>();
        new InterfaceConstraint<int>();
        new InterfaceConstraint<Nullable<int>>();
        new InterfaceConstraint<int?>();
        new InterfaceConstraint<Exception>();

        new UnresolvedTypeConstraint<string>();
        new UnresolvedTypeConstraint<String>();
        new UnresolvedTypeConstraint<int>();
        new UnresolvedTypeConstraint<Nullable<int>>();
        new UnresolvedTypeConstraint<int?>();
        new UnresolvedTypeConstraint<Enum>();
        new UnresolvedTypeConstraint<Enum1>();
    }
}
