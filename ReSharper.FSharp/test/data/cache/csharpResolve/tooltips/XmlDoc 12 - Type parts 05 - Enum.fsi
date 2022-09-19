module Test

/// A signature
type A = Red=0 | Yellow=1 | Blue=2

[<AutoOpen>]
module Ext =
    /// A augment signature
    type A with
        static member M2: unit -> unit
