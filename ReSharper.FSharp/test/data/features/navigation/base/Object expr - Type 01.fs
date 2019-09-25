type A() = class end

type B() =
    inherit A()

{ new {on}B() with
      override x.ToString() = "" }
