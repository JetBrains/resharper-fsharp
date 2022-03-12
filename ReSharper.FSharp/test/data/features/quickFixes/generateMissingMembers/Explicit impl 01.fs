[<AbstractClass>]
type A() =
    abstract Dispose: unit -> unit
    interface System.IDisposable with
        member this.Dispose() = failwith "todo"

type B{caret}() =
    inherit A()
