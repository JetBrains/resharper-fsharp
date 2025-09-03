module Module

type A =
    member _.M{caret}([<Attr>] ?x) = x.Value + 1
