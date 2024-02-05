type I<'T1> =
    abstract M{on}: 'T1 -> unit

type T() =
    member this.M<'T2>() =
        { new I<'T2> with
            member _.M(x: 'T2) = ()
        }
