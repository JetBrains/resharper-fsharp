[<AbstractClass>]
type NotAnInterface() =
    abstract member Print1 : unit -> unit
    member this.Print2() = printfn "Definitely not an interface"

type SomeClass1(x: int, y: float) =
   interface NotAnInterface{off}