module Inline

type T =
    static member Prop1 = 1
    static member Prop2 = 2

let eq a b =
    a = b

let run () =
    let _ = not (eq T.Prop1 T.Prop2)

    ()
