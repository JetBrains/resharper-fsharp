// ReSharper disable once CheckNamespace
public class Class
{
    protected int ProtectedProp => 1;
    protected int ProtectedMethod() => 1;

    protected class Nested
    {
        public int NestedProp => 1;
        public int NestedMethod() => 1;
    }
}
