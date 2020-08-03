namespace Test

open System.Runtime.CompilerServices

type TestType() =

    [<{caret}Extension>]
    let a() = 5
