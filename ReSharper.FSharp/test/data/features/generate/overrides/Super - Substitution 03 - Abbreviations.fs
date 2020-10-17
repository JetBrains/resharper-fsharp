// ${KIND:Overrides}
// ${SELECT0:M(T1):System.Void}
// ${SELECT1:M(T2):System.Void}

[<AbstractClass>]
type A<'T1>() =
    abstract M: 'T1 -> unit

type a = A<int>

[<AbstractClass>]
type B<'T2>() =
    inherit a()
    abstract M: 'T2 -> unit

type b = B<double>

type T() ={caret}
    inherit b()
