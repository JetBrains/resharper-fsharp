type I<'T> =
    abstract M{on}: 'T -> unit

{ new I<'b> with
    member _.M(i: 'b) = ()
}
