// ${COMPLETE_ITEM:Match values}
[<RequireQualifiedAccess>]
module Module1 =
    type U =
        | A

module Module2 =
    match Module1.A with
    | {caret}
