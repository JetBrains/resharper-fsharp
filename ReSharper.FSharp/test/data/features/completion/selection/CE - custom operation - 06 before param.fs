module Module

type CE() =
    member this.Yield(x) = x
    member this.Return(x) = x
    [<CustomOperation("custom")>]
    member this.Custom(x, _: unit) = x

let ce = CE()

ce {
    c{caret} ()
    return 0
}
