// ${KIND:Overrides}
// ${SELECT0:E:Microsoft.FSharp.Control.FSharpHandler`1[T -> System.Int32]}

[<AbstractClass>]
type A() =
    [<CLIEvent>]
    abstract E: IEvent<int>

type T() ={caret}
    inherit A()
