module Say

[<RequireQualifiedAccess>]
type U =
    | A
    | B
    | C of x: int

match U.A{caret} with
| U.A -> ()
