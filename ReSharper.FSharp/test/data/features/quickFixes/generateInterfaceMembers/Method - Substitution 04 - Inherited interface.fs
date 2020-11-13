module Module

type IA<'T1> =
  abstract M: 'T1 * double -> unit

type IB<'T2> =
  inherit IA<double>
  abstract M: 'T2 * int -> unit

type T() =
  interface IB<int>{caret}
