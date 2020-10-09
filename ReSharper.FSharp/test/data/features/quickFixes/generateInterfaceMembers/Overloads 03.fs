module Module

type I =
  abstract M1: int -> unit
  abstract M1: double -> unit
  abstract M2: int -> unit

type T() =
  interface I{caret}
