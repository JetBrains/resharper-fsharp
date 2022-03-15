module Module

type I =
    abstract Item: int -> string with get, set

type T() =
  interface I{caret}
