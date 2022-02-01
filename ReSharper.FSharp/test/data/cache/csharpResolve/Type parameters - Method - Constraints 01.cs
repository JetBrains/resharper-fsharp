using System;

class C : I
{
    public void M1<T>(T value) where T : ICloneable =>
        throw new NotImplementedException();

    public void M2<T>(T value) where T : struct =>
        throw new NotImplementedException();
}
