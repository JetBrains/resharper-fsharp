using System;

public class Class
{
    public delegate void VoidDelegate();
    public static event VoidDelegate VoidEvent;

    public static event EventHandler Event;

    public delegate void IntHandler(object sender, int i);
    public static event IntHandler IntEvent;
}
