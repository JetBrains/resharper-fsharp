// ${FILTER_ITEMS:e,f}
type T() = class end

[<AutoOpen>]
module E =
    type T with
        member this.Ext(e: int, f: int) = ()

let t = T()
t.Ext({caret})
