type I<'T> =
    abstract M: 'T -> unit

type T() =
    member this.M<'T>() =
        { new I<'T> with
            member _.M{on}(x: 'T) = ()
        }
