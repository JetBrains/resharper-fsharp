using System;

public struct S
{
}

public class C1
{
}

public class C2
{
    internal C2()
    {
    }
}

public class C3
{
    public C3(int i)
    {
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        new DefaultCtorConstraint<int>();
        new DefaultCtorConstraint<S>();
        new DefaultCtorConstraint<C1>();
        new DefaultCtorConstraint<C2>();
        new DefaultCtorConstraint<C3>();
        
        new DefaultCtorConstraint1<int>();
        new DefaultCtorConstraint1<S>();
        new DefaultCtorConstraint1<C1>();
        new DefaultCtorConstraint1<C2>();
        new DefaultCtorConstraint1<C3>();
        
        new DefaultCtorConstraint2<int>();
        new DefaultCtorConstraint2<S>();
        new DefaultCtorConstraint2<C1>();
        new DefaultCtorConstraint2<C2>();
        new DefaultCtorConstraint2<C3>();
        
        new DefaultCtorConstraint3<int>();
        new DefaultCtorConstraint3<S>();
        new DefaultCtorConstraint3<C1>();
        new DefaultCtorConstraint3<C2>();
        new DefaultCtorConstraint3<C3>();
        
        new DefaultCtorConstraint4<int>();
        new DefaultCtorConstraint4<S>();
        new DefaultCtorConstraint4<C1>();
        new DefaultCtorConstraint4<C2>();
        new DefaultCtorConstraint4<C3>();
    }
}
