namespace Test

module A =
    open System.Collections.Generic
    open System.IO

    let x = Queue<Stream>()

module B =
    open A

    let y{caret} = x
