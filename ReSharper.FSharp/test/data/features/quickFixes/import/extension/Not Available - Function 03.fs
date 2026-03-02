module Module

module Nested =
    open System.Runtime.CompilerServices

    [<Extension; CompiledName "F">]
    let f (s: string) = ()

"".F{caret}()
