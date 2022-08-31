module Test

/// Interface impl
[<Interface>]
type Interface =
    abstract member M1: unit

[<AutoOpen>]
module Ext =
    /// Interface augment impl
    type Interface with
        static member M2() = ()
