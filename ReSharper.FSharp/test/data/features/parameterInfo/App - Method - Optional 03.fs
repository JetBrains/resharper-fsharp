open System.Runtime.InteropServices

type T =
    static member M([<Optional; DefaultParameterValue null>] a: int option) = ()

T.M({caret})
