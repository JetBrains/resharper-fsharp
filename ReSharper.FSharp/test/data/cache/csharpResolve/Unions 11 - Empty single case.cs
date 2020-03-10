public class Class1
{
    public Class1()
    {
        U a = U.A;
        int uTags = U.Tags.A;
        int aTag = a.Tag;
        int isA = a.IsA;
        bool equalsU = a.Equals(a);
        bool equalsObj = a.Equals("");
        bool equalsObjWithComparer = a.Equals("", null);

        F(a, a);
    }

    public static void F<T1, T2>(T1 t1, T2 t2)
        where T1 : class
        where T2 : struct
    {
    }
}
