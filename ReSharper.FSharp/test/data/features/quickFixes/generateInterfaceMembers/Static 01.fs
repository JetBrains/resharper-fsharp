module Module

type I =
  static abstract M: a: int -> b: int -> unit

type T() =
  interface I{caret}
