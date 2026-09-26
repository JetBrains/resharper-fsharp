// ${COMPLETE_ITEM:member M(int)}
module Module

[<Interface>]
type ITest<'T> =
    abstract M: 'T -> unit
    abstract M: string -> unit

let a =
    { new ITest<int> with
        member this.M(str: string) = ""
        {caret} }