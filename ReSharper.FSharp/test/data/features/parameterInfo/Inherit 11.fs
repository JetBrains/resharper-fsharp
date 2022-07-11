type A(i: int, s: string) = class end

type B() =
    inherit{caret} A(1, "")
