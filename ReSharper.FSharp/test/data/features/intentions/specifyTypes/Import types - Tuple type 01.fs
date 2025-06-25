namespace Test

module A =
    open System.IO

    let x = StreamReader(""), "".GetType()

module B =
    open A

    let y{caret} = x
