module Kke

open JetBrains.Annotations

type A() =
    [<RegexPattern>]
    member x.R = "[123]"

    member x.K = "[123]"

let f ([<RegexPattern>] x: string) = ()
f ("[123]")

let g (x: string) = ()
g ("[123]")
