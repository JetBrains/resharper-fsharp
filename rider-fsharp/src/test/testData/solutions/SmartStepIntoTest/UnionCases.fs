module UnionCases

type U =
    | A of int option * int * int

type T =
    static member Prop = 1

let f1 (u: U) = ()
let f2 (i: int option) = ()

let run () =
    f1 (A(Some 1, T.Prop, T.Prop))
    f2 (Some 1)
    [ f2 None; f2 (Some 1); f2 (Some 2) ], [f2 (Some 3); f2 None]
