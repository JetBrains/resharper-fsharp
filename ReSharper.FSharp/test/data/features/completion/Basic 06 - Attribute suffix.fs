// ${COMPLETE_ITEM:Attributes}
module Module

type R =
    { Attributes: string list }

let r = Unchecked.defaultof<R>
r.A{caret}
