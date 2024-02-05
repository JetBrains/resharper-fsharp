type I<'T> =
    abstract M: 'T -> unit

type T(param) =
    let i =
        { new I<_> with
            member _.M{on} x = ()
        }

    do i.M param
