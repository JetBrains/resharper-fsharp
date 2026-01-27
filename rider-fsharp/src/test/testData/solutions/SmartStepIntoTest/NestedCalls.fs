module NestedCalls

type T =
    static member Prop1 = 1
    static member Prop2 = 2

    static member M1() = 1
    static member M2(a) = a + 1
    static member M3(a, b) = a + b


let f1 a =
    a + 1 |> ignore

let f2 a b =
    a + b |> ignore



let run () =
    f1 T.Prop1
    f1 (T.M1())
    f1 (T.M2(T.Prop1))
    f1 (T.M2(T.Prop1 + T.Prop2))
    f1 (T.M3(T.Prop1, T.Prop2))
    f2 T.Prop1 T.Prop2
    f2 T.Prop1 T.Prop2

    let _ = T.Prop1 + T.Prop2

    let g1 a b =
        a + b |> ignore

    let g2 a b =
        a + 1 |> ignore

    g1 T.Prop1 T.Prop2
    g2 T.Prop1 T.Prop2

    ()
