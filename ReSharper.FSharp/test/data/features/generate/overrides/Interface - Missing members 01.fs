// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}

type I =
    abstract M: int -> unit
    abstract P: int

type T() ={caret}
    interface I
