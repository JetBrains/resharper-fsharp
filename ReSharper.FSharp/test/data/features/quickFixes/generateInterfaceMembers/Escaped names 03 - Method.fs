module Module

type I =
  abstract ``My method``: unit -> unit

type T() =
  interface I{caret}
