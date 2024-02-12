// ${KIND:Overrides}
// ${SELECT0:M(System.Int32,System.Int32,System.String,System.Double,System.Double):System.Void}

type T() =
    abstract M: int * int -> string -> double * double -> unit
    default this.M(i1, i2) s (d1, d2) = ()

{ new T() with{caret} }
