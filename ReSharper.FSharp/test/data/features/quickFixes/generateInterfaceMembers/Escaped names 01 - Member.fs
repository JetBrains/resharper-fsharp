module Module

type I =
  abstract ``My prop``: int
  abstract ``type``: int

type T() =
  interface I{caret}
