module Module

type B<'T> =
    { X: 'T }

type R =
    { ...B<int>
      Y: string }

type G<'U> =
    { ...B<'U>
      Z: int }

type BInt = B<int>

type RAbbr =
    { ...BInt
      W: bool }

type R2 =
    { ...G<string>
      P: int }
