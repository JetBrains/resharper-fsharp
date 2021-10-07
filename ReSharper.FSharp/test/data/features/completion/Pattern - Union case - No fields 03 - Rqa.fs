// ${COMPLETE_ITEM:U.A}
module Module

[<RequireQualifiedAccess>]
type U =
    | A

match U.A with
| {caret}
