type I<'T> =
    abstract M{on}: 'T -> unit

type T() =
    member this.M<'T>() =
        { new I<'T> with
            member _.M(x: 'T) = ()
        }
