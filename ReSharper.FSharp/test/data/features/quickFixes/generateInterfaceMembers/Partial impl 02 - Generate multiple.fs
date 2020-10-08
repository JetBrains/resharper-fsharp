module Module

type I =
  abstract P1: int
  abstract P2: int
  abstract P3: int

type T() =
  interface I{caret} with
    member x.P1 = 1
