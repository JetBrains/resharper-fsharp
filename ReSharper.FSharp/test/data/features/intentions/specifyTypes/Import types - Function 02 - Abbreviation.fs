namespace Ns

open System.Text

type StringExpr<'T> = StringBuilder -> 'T


namespace Test

module A =
    open Ns

    let f (_: string): StringExpr<unit> = failwith ""

module B =
    open A

    let g{caret} = f
