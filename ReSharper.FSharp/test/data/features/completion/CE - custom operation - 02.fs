// ${COMPLETE_ITEM:custom}
module Module

type CE() =
    member this.Yield(x) = x
    [<CustomOperation("custom")>]
    member this.Custom(x) = x

let ce = CE()

ce {
    for i in 1..10 do
        c{caret}
}
