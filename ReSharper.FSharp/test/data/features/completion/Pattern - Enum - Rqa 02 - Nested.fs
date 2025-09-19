// ${ABSENT_ITEM:A}
module Module

[<RequireQualifiedAccess>]
module E =
    [<RequireQualifiedAccess>]
    module E =
        type E =
            | A = 1

match E.E.E.A with
| E.E.E.{caret}A -> ()
