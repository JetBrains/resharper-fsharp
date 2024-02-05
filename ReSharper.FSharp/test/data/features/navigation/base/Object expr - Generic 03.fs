type I<'T> =
    abstract M: 'T -> unit

{ new I<_> with
    member _.M{on} i = ()
}
