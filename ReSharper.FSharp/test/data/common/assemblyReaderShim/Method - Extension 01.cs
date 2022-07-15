using System.Collections.Generic;

public static class Class
{
    public static void StringExt(this string s)
    {
    }

    public static void ObjSeq(this IEnumerable<object> e)
    {
    }
    
    public static void GenericSeqExt<T>(this IEnumerable<T> e)
    {
    }
    
    public static void StringSeqExt(this IEnumerable<string> e)
    {
    }

}
