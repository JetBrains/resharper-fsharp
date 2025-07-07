namespace Test

open System.Collections

module A =
    open System.Collections.Generic

    let x = Queue<int>()

module B =
    open A

    let y{caret} = x
