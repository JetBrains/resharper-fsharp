open System.Runtime.InteropServices

type T =
    static member M([<Optional; DefaultParameterValue 1>] a: int) = ()

T.M({caret})
