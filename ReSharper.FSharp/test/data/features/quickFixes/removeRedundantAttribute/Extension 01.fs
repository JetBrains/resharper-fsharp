module Module

open System.Runtime.CompilerServices

[<Extension{caret}>]
type T() =
    member this.P = 1
