namespace Test

module A =
    open System.IO

    let x = {| A = ["".GetType()]; B = StreamReader("") |}

module B =
    open A

    let y{caret} = x
