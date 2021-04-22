public class Class
{
    public static int StaticGet { get; }
    public static int StaticGetSet { get; set; }

    public static int StaticSet
    {
        set { }
    }

    public static int StaticExpressionBody => 1;
    public static int StaticProp { get; set; } = 1;

    public int Get { get; }
    public int GetSet { get; set; }
    public int GetGet { get; get; }

    public int GetInit { get; init; }

    public int Init
    {
        init { }
    }

    public static int SetWithoutBody { set; }
    public static int InitWithoutBody { init; }

    public static int WrongAccessorName1 { get1; }
    public static int WrongAccessorName2 { get; set1; }
    public static int NoAccessors { }
}
