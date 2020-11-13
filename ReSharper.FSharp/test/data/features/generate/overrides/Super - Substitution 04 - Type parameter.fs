// ${KIND:Overrides}
// ${SELECT0:M(T1):System.Void}

[<AbstractClass>]
type A<'T1>() =
    abstract M: 'T1 -> unit
    abstract M: double -> unit

[<AbstractClass>]
type T<'T2>() ={caret}
    inherit A<'T2>()
