module Module

type I =
  abstract M: unit -> unit

type T() =
  interface I{caret}
