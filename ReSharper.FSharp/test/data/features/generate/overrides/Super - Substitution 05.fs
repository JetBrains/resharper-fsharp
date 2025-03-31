// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

[<AbstractClass>]
type A<'T1>() =
    abstract P: 'T1

[<AbstractClass>]
type T() ={caret}
    inherit A<int>()

    override this.P = 1
