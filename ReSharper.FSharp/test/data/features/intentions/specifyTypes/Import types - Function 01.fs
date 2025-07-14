namespace Test

module A =
    open System.IO
    open System.Collections

    let x = fun (_: Stream) -> fun (_: BitArray) -> "".GetType()

module B =
    open A

    let y{caret} = x
