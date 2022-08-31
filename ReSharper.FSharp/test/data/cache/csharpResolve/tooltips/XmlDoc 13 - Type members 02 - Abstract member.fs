module Test

[<AbstractClass>]
type A =
    /// M 
    abstract member M: unit -> unit
    /// M default
    default x.M() = ()
