module Module

type I =
    abstract Item: index: int * foo: bool -> string with get, set

type T() =
  interface I{caret}
