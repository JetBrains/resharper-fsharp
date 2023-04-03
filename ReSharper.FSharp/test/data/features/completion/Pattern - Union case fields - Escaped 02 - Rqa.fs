// ${COMPLETE_ITEM:i}
module Module

[<RequireQualifiedAccess>]
type U<'T> =
    | A
    | ``B C`` of 'T

match U<int>.A with
| U.``B C``({caret})
