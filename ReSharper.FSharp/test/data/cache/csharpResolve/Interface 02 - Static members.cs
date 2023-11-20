public class C11 : T1
{
    public int P1 => T1.P2;
}

public class C12 : T1
{
    int T1.P1 => T1.P2;
}

public class C21 : T2
{
    public int P1 => T1.P2;
}

public class C22 : T2
{
    int T2.P1 => T2.P2;
}

public class C31 : T3<int>
{
    public int P1 => T1.P2;
}

public class C32 : T3<string>
{
    string T3<string>.P1 => T3<string>.P2;
}
