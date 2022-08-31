module Test

/// Interface signature
[<Interface>]
type Interface =
    abstract member M1: unit

[<AutoOpen>]
module Ext =
    /// Interface augment signature
    type Interface with
        static member M2: unit -> unit
