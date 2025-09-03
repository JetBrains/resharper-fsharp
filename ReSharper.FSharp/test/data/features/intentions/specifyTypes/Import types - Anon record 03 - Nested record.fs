namespace Test

module A =
    open System.IO

    let x = {| A = {| A = "".GetType(); B = MemoryStream() |}; B = 4 |}

module B =
    open A

    let y{caret} = x
