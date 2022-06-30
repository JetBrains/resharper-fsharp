type A() =
    new (i: int, s: string) = A()
    new (i: int) = A()

type B() =
    inherit A(1, ""{caret})
