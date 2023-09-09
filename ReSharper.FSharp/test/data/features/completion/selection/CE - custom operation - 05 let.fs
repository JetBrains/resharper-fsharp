module Module

type CE() =
    member this.Yield(x) = x
    [<CustomOperation("custom")>]
    member this.Custom(x) = x

let ce = CE()

ce {
    let x = 1
    {caret}
}
