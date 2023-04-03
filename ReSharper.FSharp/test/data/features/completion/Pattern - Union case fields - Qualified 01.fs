// ${COMPLETE_ITEM:i, i1}
module Module

type U<'T> =
    | A
    | Bb of int * 'T

match U<int>.A with
| U.Bb({caret})
| U.Bb i -> ()
