namespace Ns

type Type() = class end


namespace Test

open System

module A =
    open Ns

    let x: Type = Type()

module B =
    open A

    let y{caret} = x
