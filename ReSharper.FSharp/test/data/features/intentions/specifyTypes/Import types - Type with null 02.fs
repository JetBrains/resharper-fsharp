namespace Test

module A =
    type Type = class end
    let x: Type | null = null

module B =
    open System

    let y{caret} = A.x
