module Module

type T<'TT>() =
    member x.Method<'TM>() =
        Unchecked.defaultof<'TT>, Unchecked.defaultof<'TM>

