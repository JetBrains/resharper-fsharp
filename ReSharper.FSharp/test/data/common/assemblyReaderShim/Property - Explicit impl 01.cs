public interface I<T>
{
    T P { get; }
}

public class C1 : I<int>, I<string>
{
    int I<int>.P => 1;
    string I<string>.P => "";
}

public class C2<T> : I<T>
{
    T I<T>.P => null;
}
