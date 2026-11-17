module Module

type System.String with
    member this.M(a: int, b: int) = ()

"".M(1, ""{caret})
