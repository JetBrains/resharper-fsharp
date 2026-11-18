// ${KIND:Overrides}
// ${SELECT0:M1():System.Void}
// ${SELECT1:M2():System.Void}

type IA =
    abstract M1: unit -> unit

type IB =
    inherit IA
    abstract M2: unit -> unit

{ new IB with{caret} }
