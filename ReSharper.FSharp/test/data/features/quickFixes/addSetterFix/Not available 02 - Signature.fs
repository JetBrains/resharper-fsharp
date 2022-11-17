module Test

type T() =
    member val P = 42
    member x.M() = x.P{caret} <- 23
