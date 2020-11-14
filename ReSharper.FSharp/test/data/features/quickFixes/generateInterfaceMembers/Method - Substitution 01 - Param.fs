module Module

type I<'T> =
  abstract M: 'T -> unit
  abstract M: double -> unit

type T() =
  interface I<int>{caret}
