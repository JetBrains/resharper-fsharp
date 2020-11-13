module Module

type I =
  abstract M<'T1> : int -> 'T1
  abstract M<'T1, 'T2> : double -> 'T2

type T() =
  interface I{caret}
