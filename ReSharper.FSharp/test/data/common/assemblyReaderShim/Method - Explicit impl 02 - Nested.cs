public class Class
{
    public interface I<T>
    {
        void M();
    }

    public class Nested1 : I<int>, I<string>
    {
        int I<int>.M() => 1;
        string I<string>.M() => "";
    }

    public class Nested2<T> : I<T>
    {
        T I<T>.M() => null;
    }
}
