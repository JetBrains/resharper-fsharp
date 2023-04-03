// ${COMPLETE_ITEM:i1, i2}
module Module

[<RequireQualifiedAccess>]
type U<'T> =
    | A
    | B of int * 'T

match 1, (U<int>.A) with
| i, U.B({caret})
