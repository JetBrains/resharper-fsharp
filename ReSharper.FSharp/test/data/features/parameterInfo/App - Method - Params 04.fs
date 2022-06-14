open System

type T =
    static member M(a, [<ParamArray>] b: int[]) = ()

T.M(1, 2{caret})
