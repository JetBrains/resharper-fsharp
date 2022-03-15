module Module

type I =
  abstract P1: int with get, set
  abstract P2: int

type T() =
  interface I{caret} with
    member val P1 = 1 with get, set
