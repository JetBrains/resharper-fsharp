open System

type T =
    static member M([<ParamArray>] i: int[]) = ()

T.M({caret})
