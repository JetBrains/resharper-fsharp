// ${COMPLETE_ITEM:M(int)}
module Module

[<Interface>]
type ITest<'T> =
    abstract M: 'T -> unit
    abstract M: string -> unit

let a =
    { new ITest<int> with
        member this.M(str: string) = ""
        member this.{caret} }