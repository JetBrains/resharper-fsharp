module Module

[<RequireQualifiedAccess>]
type U1 =
    | A1

type U2 =
    | A2
    | B2 of U1

match A2 with
| B2(U1.{caret}) -> ()
