// ${COMPLETE_ITEM:i}
module Module

type U<'T> =
    | A
    | ``B C`` of 'T

match U<int>.A with
| ``B C``({caret})
