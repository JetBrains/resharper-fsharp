module Module

type I =
  abstract M<'T1, 'T2> : int -> unit

type T() =
  interface I{caret}
