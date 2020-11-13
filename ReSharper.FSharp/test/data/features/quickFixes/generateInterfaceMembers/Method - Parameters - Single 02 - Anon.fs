module Module

type I =
  abstract M: int -> unit

type T() =
  interface I{caret}
