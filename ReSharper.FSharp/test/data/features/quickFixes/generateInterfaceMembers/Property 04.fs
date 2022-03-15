module Module

type I =
  abstract P: int with get, set

type T() =
  interface I{caret} with
    member x.P = 1
