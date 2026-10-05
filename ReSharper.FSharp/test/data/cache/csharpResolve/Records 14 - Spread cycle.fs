module Module

type A =
    { ...B
      X: int }

and B =
    { ...A
      Y: int }
