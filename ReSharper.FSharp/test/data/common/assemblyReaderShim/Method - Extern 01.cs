using System.Runtime.InteropServices;

public class Class
{
    public static extern int ExternMethod1(int i);

    public static extern int ExternMethod2(IntPtr i, string s);

    [DllImport("Foo.dll")]
    public static extern int ExternMethod3(int i);
}
