type I<'T> =
    abstract M{on}: 'T -> unit

type T(param: int) =
    let i =
        { new I<_> with
            member _.M x = ()
        }

    do i.M param
