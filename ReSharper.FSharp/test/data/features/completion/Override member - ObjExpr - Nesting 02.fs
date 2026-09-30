// ${COMPLETE_ITEM:member Foo()}
module Module

[<Interface>]
type ITest =
    abstract Foo: unit -> obj

[<AbstractClass>]
type Base() =
    abstract P: obj

{ new Base() with
     override x.P = 
        { new ITest with
            {caret} } }
