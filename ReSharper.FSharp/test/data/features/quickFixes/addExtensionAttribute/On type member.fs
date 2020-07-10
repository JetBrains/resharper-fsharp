namespace Test

open System.Runtime.CompilerServices

type TestType() =

    [<{caret}Extension>]
    static member a() = 5
