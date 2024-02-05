type I<'T> =
    abstract M{on}: 'T -> unit

{ new obj() with
      override this.ToString() = ""

  interface I<_> with
      member _.M x = ()
}
