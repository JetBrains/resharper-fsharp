namespace Ns

[<RequireQualifiedAccess>]
module System =
    type Type() = class end


namespace Test

module A =
    open Ns

    let x: System.Type = System.Type()

module B =
    open A

    let y{caret} = x
