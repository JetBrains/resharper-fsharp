// ${COMPLETE_ITEM:member Dispose()}
module Module

[<Interface>]
type ITest =
    abstract Foo: unit -> obj

[<AbstractClass>]
type Base() =
    abstract P: obj

{ new Base() with
     override x.P = ()
     interface ITest with
         member x.Foo() = 
            { new System.IDisposable with
                {caret} } }
