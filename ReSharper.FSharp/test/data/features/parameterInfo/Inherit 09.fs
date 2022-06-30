type A(i: int, s: string) = class end

type B() =
    inherit A{caret}(1, "")
