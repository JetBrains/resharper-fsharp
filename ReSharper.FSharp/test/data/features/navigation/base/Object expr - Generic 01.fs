type I<'T> =
    abstract M: 'T -> unit

{ new I<int> with
    member _.M{on} i = ()
}
