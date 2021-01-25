type I =
    abstract M: unit -> unit

[<AbstractClass>]
type A<'T>() =
    abstract M: 'T -> unit
 
    interface I with
        member this.M() = failwith "todo"

type {caret}B() =
    inherit A<int>()
