type A(i: int, s: string) = class end

type B() =
    inherit A(1, "",{caret})
