module Async

open System.Threading

type T =
    static member Prop = 1

let f1 x =
    x + 1

let f2 a b =
    a + b

let a =
    async { return 1 }

let incrementAsync i =
    async { return i + 1 }

let a1 =
    async {
        return f1 T.Prop
    }

let a2 =
    async {
        let! aResult = a
        return f2 T.Prop aResult
    }

let a3 =
    async {
        let i = 1
        return f2 i T.Prop
    }

let a4 =
    async {
        let! a = incrementAsync T.Prop
        let i = a + 1
        return f2 i T.Prop
    }

let a5 =
    async {
        let! a = a
        let i =
            ignore a
            a + 1

        return f2 i T.Prop
    }

let a6 =
    async {
        let! a = a
        let i =
            a + 1 |> ignore
            1

        return f2 i T.Prop
    }

let a7 =
    async {
        let! a = incrementAsync T.Prop
        return a
    }

let a8 =
    async {
        return! incrementAsync T.Prop
    }

let run () =
    let computations = [a1; a2; a3; a4; a5; a6; a7; a8]
    for a in computations do
        Async.RunSynchronously(a) |> ignore
