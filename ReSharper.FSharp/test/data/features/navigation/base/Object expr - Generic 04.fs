type I<'T> =
    abstract M: 'T -> unit

{ new I<'b> with
    member _.M{on}(i: 'b) = ()
}
