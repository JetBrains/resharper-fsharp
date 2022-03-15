module Module

type I =
    abstract Item: index: int -> string with get, set

type T() =
  interface I{caret}
