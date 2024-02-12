// ${KIND:Overrides}
// ${SELECT0:M(System.Int32,System.Double):System.Void}

type T() =
    abstract M: int -> double -> unit
    default this.M i d = ()

{ new T() {caret} }
