[<RequireQualifiedAccess>]
type U =
    | A of field: int

let a{caret} = U.A 1
