module Module

type CE() =
    member this.Yield(x) = x
    [<CustomOperation("custom")>]
    member this.Custom(x) = x

let ce = CE()

let foo f = ()

let car = ()

ce {
    let x =
        let y = 1
        {caret}
    custom
}
