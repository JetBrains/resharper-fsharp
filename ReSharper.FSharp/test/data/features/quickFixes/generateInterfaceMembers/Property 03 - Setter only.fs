module Module

type I =
  abstract P: int with set

type T() =
  interface I{caret}
