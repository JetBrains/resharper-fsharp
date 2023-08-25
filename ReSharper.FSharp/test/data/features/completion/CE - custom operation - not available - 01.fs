// ${COMPLETE_ITEM:car}
module Module

type CE() =
    member this.Yield(x) = x
    [<CustomOperation("custom")>]
    member this.Custom(x) = x

let ce = CE()

module M =
    let car = ()

ce {
    do c{caret}
}
