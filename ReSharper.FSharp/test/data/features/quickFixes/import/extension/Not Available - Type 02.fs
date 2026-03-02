module Module

open System.Runtime.CompilerServices

module Nested =
    type Extensions =
        [<Extension>]
        static member Method(i: int) = ()

"".Method{caret}()
