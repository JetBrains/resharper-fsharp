module Module

module Nested =
    [<System.Runtime.CompilerServices.Extension>]
    let f (s: string) = ()

"".f{caret}()
