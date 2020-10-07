module Module

type I =
  abstract M<'T> : int -> unit

type T() =
  interface I{caret}
