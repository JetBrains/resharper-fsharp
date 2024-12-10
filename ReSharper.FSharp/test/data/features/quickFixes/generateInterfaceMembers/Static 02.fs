module Module

type I =
  static abstract P: int with set

type T() =
  interface I{caret}
