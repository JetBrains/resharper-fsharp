type I<'T> =
    abstract M{on}: 'T -> unit

type T<'T>() =
    let _ =
        { new I<'T> with
            member _.M(x: 'T) = ()
        }
