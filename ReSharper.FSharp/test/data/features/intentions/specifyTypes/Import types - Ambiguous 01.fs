namespace System

type Type() = class end


namespace Test

module A =
    open System

    let x: Type = Type()

module B =
    open A

    let y{caret} = x
