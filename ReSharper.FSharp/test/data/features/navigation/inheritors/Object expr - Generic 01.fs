type I<'T> =
    abstract M{on}: 'T -> unit

{ new I<int> with
    member _.M i = ()
}
