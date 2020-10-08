module Module

type I =
  abstract M: a: int * b: int -> c: int -> unit

type T() =
  interface I{caret}
