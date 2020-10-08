module Module

type IA<'T1> =
  abstract M: 'T1 * int -> unit

type IB<'T2, 'T3> =
  inherit IA<'T2>
  abstract M: 'T3 * double -> unit

type T() =
  interface IB<int, double>{caret}
