module Module

type B<'T> =
    { X: 'T }

type BAbbr<'U> = B<'U>

type R =
    { ...BAbbr<int>
      Y: string }

type G<'W> =
    { ...BAbbr<'W>
      Z: int }

type BAbbrInt = BAbbr<int>

type R2 =
    { ...BAbbrInt
      P: bool }
