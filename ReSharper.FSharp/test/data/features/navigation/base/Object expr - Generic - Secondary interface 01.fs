type I<'T> =
    abstract M: 'T -> unit

{ new obj() with
      override this.ToString() = ""

  interface I<_> with
      member _.M{on} x = ()
}
