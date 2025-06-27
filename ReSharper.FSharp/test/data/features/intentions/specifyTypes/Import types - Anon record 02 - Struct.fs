namespace Test

module A =
    open System.IO

    let x = struct {| A = ["".GetType()]; B = StreamReader("") |}

module B =
    open A

    let y{caret} = x
