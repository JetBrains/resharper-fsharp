module Module

type I =
  abstract M: int -> unit
  abstract M: int * int -> unit

type T() =
  interface I{caret}
