module Test

[<AbstractClass>]
type A =
    /// M signature
    abstract member M: unit -> unit
    /// M signature default
    default M: unit -> unit
