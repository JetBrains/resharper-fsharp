// ${KIND:Overrides}
// ${SELECT0:M(System.Int32,System.Double,System.Double):System.Void}

type T() =
    abstract M: int -> double * double -> unit
    default this.M i1 (d1, d2) = ()

{ new T() with{caret} }
