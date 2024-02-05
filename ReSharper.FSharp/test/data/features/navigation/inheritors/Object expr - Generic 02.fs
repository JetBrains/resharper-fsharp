type I<'T> =
    abstract M{on}: 'T -> unit

{ new I<_> with
    member _.M(i: int) = ()
}
