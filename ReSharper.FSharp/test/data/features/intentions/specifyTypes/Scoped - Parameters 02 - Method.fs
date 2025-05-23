// ${RUN:Annotate all parameters}
module Module

type A() =
    member _.M a (b, [<Attr>]?c{caret}) = ()
