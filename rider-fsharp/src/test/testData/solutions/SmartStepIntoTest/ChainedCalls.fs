module ChainedCalls

type T(i: int) =
    static member Prop = 1
    
    member this.Prop1 = 1
    member this.Prop2 = 1

    member this.M1() = this
    member this.M2() = this
    member this.M3() = this

    member this.M4(a) =
        a + 1 |> ignore
        this

    member this.M5(a, b) =
        a + b |> ignore
        this

let run () =
    let t1 = T(T.Prop)
    let t2 = new T(T.Prop)

    let _ = t1.M1().M2().M3()
    let _ = t1.M4(t1.Prop1)
    let _ = t1.M5(t1.Prop1, t1.Prop2).M1()

    ()
