module Module

type A =
  abstract P1: int

type B =
  inherit A
  abstract P2: int

type T() =
  interface B{caret}
