module Module

type I =
  abstract P1: int with get, set
  abstract P2: int with get, set

type T() =
  interface I{caret}
