module Module

type A() =
    member x.Prop = 3

A() |> _.Prop{caret}
