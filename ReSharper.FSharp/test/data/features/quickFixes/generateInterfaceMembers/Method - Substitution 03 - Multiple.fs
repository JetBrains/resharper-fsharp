module Module

type I<'T, 'Y> =
  abstract M: int -> 'T
  abstract M: double -> 'Y

type T() =
  interface I{caret}<int, double>
