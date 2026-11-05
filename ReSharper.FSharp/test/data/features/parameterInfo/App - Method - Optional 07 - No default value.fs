open System.Runtime.InteropServices

type T =
    static member M([<Optional>] a: int) = ()

T.M({caret})
