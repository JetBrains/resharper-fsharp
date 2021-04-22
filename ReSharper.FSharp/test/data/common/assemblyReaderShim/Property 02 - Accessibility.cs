public class Class
{
    public static int PublicPropPublicGetPublicSet { public get; public set; }

    public static int PublicPropPrivateSet { get; private set; }
    public static int PublicPropProtectedSet { get; protected set; }

    public static int PublicPropProtectedGetProtectedSet { protected get; protected set; }
    public static int PublicPropPrivateGetPrivateSet { private get; private set; }

    protected static int ProtectedPropPublicGet { public get; set; }
    protected static int ProtectedPropPublicSet { get; public set; }

    protected static int ProtectedPropPrivateGet { private get; set; }
    protected static int ProtectedPropPrivateSet { get; private set; }

    protected static int ProtectedPropProtectedSet { get; protected set; }

    protected static int ProtectedPropPublicGetPublicSet { public get; public set; }

    protected static int PrivatePropPublicGetPublicSet { public get; public set; }
    protected static int PrivatePropPublicGet { public get; set; }
    protected static int PrivateProp { get; set; }
}
