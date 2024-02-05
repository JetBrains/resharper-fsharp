type I<'T1> =
    abstract M{on}: 'T1 -> unit

type T<'T2>() =
    let _ =
        { new I<'T2> with
            member _.M(x: 'T2) = ()
        }
