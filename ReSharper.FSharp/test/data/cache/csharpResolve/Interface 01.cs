public class C11 : T1
{
    public int P => 1;
}

public class C12 : T1
{
    int T1.P => 1;
}

public class C21 : T2
{
    public void Dispose() {}
}

public class C22 : T2
{
    void System.IDisposable.Dispose() {}
}

public class C31 : T3
{
    public int P1 => 1;
}

public class C32 : T3
{
    int T3.P1 => 1;
}

public class C41 : T4
{
    int P => 1;
}

public class C42 : T4
{
    int T4.P => 1;
}
