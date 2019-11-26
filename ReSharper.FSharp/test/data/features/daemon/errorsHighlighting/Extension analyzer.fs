namespace global

open System.Runtime.CompilerServices

[<Extension>]
module Module1 =
    [<Extension>]
    type T1() =
        class end

    [<Extension; AbstractClass; Sealed>]
    type T2() =
        class end

    [<Extension>]
    type T3() =
        [<Extension>]
        member x.Foo(_: int) = ()

    type T4() =
        [<Extension>]
        member x.Foo(_: int) = ()

[<Extension>]
module Module2 =
    [<Extension>]
    let foo (_: int) = ()

module Module3 =
    [<Extension>]
    let foo (_: int) = ()