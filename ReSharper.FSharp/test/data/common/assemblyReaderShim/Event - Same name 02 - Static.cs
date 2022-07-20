using System;

public delegate void VoidDelegate();

public class Class1
{    
    public event VoidDelegate Event;
    public static event EventHandler Event;
}

public class Class2
{
    public event EventHandler Event;
    public static event VoidDelegate Event;
}
