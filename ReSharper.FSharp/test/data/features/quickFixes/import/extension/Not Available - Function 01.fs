module Module

module Nested =
    open System.Runtime.CompilerServices

    [<Extension>]
    let f (s: string) = ()

"".f{caret}()
