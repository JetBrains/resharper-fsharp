using System;

public delegate void VoidDelegate();

public class Class1
{    
    public event VoidDelegate Event;
    public static event EventHandler StaticEvent;
}

public class Class2
{
    public event EventHandler Event;
    public static event VoidDelegate StaticEvent;
}
