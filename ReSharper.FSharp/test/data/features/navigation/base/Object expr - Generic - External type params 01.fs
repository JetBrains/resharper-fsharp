type I<'T> =
    abstract M: 'T -> unit

type T<'T>() =
    let _ =
        { new I<'T> with
            member _.M{on}(x: 'T) = ()
        }
