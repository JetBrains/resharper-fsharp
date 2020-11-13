module Module

type I<'T> =
  abstract M: int -> 'T
  abstract M: double -> 'T

type T() =
  interface I<int>{caret}
