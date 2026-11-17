// ${COMPLETE_ITEM:fun int ->}
module Module

open System.Runtime.CompilerServices

[<Extension>]
type E =
    [<Extension>]
    static member Each(xs: int list, action: int -> unit) = ()

[1].Each({caret})
