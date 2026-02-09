module Task

open System.Threading

type T =
    static member Prop = 1

let f1 x =
    x + 1

let f2 a b =
    a + b

let a =
    task { return 1 }

let incrementAsync i =
    task { return i + 1 }

let a1 =
    task {
        return f1 T.Prop
    }

let a2 =
    task {
        let! aResult = a
        return f2 T.Prop aResult
    }

let a3 =
    task {
        let i = 1
        return f2 i T.Prop
    }

let a4 =
    task {
        let! a = incrementAsync T.Prop
        let i = a + 1
        return f2 i T.Prop
    }

let a5 =
    task {
        let! a = a
        let i =
            ignore a
            a + 1

        return f2 i T.Prop
    }

let a6 =
    task {
        let! a = a
        let i =
            a + 1 |> ignore
            1

        return f2 i T.Prop
    }

let a7 =
    task {
        let! a = incrementAsync T.Prop
        return a
    }

let a8 =
    task {
        return! incrementAsync T.Prop
    }

let a9 =
    task {
        let! a = a
        use d = disposable
        return a + 1
    }

let run () =
    let computations = [a1; a2; a3; a4; a5; a6; a7; a8; a9]
    for a in computations do
        a.Result |> ignore
