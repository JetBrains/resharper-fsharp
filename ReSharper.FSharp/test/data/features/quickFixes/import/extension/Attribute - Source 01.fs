module Top

open System.Runtime.CompilerServices

module Nested =
    type Extensions =
        [<Extension>]
        static member Method(s: string) = ()

"".Method{caret}
