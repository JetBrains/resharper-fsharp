[<RequireQualifiedAccess>]
module TopModule

[<RequireQualifiedAccess>]
type U =
    | A
    | B

[<RequireQualifiedAccess>]
module U =
    let _ =
        match U.A{caret} with
        | U.A -> ()
