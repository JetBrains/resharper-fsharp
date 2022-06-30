open System

type T =
    static member M(a, [<ParamArray>] b: int[], c) = ()

T.M(1, 2, 3{caret})
