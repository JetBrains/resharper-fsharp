using System;

public class Program
{
    public static void Main(string[] args)
    {
        new StructConstraint<int>();
        new StructConstraint<string>();
        new StructConstraint<String>();
        new StructConstraint<Nullable<int>>();
    }
}
