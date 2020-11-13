// ${KIND:Overrides}
// ${SELECT0:M(T1):System.Void}
// ${SELECT1:M(T2):System.Void}

[<AbstractClass>]
type A<'T1>() =
    abstract M: 'T1 -> unit

[<AbstractClass>]
type B<'T2>() =
    inherit A<int>()
    abstract M: 'T2 -> unit

type T() ={caret}
    inherit B<double>()
