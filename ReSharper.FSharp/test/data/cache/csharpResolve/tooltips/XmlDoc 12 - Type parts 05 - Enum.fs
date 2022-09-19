module Test

/// A impl
type A = Red=0 | Yellow=1 | Blue=2

[<AutoOpen>]
module Ext =
    /// A augment impl
    type A with
        static member M2() = ()
