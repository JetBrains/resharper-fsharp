// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}

type Base() =
    abstract M: ``process``: int -> unit
    default this.M(``process``: int) = ()

{ new Base() with {caret} } |> ignore
