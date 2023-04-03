// ${COMPLETE_ITEM:U.A}
module Module

[<RequireQualifiedAccess>]
type U =
    | A of int * int

    static member P =
        match A(1, 2) with
        | {caret}
