module Module

type R =
    { Foo: int
      mutable Bar: int }

    override x.ToString() = ""
    override x.Equals(obj: obj) = true
    override x.GetHashCode() = 123
