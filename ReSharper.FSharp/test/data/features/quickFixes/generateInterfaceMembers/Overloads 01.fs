module Module

type I =
  abstract M: int -> unit
  abstract M: double -> unit

type T() =
  interface I{caret}
