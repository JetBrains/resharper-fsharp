using System;

public class Program
{
    public static void Main(string[] args)
    {
        new ReferenceConstraint<string>();
        new ReferenceConstraint<String>();
        new ReferenceConstraint<int>();
        new ReferenceConstraint<Nullable<int>>();

        new NullConstraint<string>();
        new NullConstraint<String>();
        new NullConstraint<int>();
        new NullConstraint<Nullable<int>>();
    }
}
