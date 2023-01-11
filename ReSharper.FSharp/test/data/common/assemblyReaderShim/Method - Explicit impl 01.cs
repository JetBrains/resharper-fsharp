public interface I1
{
    void M();
}

public interface I2<T>
{
    T M();
}

public class C1 : I1
{
    I1.M() {}
}

public class C2 : I2<int>, I2<string>
{
    int I2<int>.M() => 1;
    string I2<string>.M() => "";
}

public class C3<T> : I2<T>
{
    T I2<T>.M() => null;
}
