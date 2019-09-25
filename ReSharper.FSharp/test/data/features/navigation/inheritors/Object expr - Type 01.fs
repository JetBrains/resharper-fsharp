type A() = class end

type {on}B() =
    inherit A()

{ new B() with
      override x.ToString() = "" }
