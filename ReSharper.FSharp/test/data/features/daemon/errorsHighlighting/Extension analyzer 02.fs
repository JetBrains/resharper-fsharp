namespace global

open System.Runtime.CompilerServices

[<Extension>]
module Module1 =
    [<Extension>]
    type T1() =
        class end

    [<Extension>]
    type T2() =
        [<Extension>]
        static member Foo(_: int) = ()

    type T3() =
        [<Extension>]
        static member Foo(_: int) = ()

    [<Extension>]
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

[<Extension>]
module Module4 =
    type System.String with
        [<Extension>]
        member _.M() = ()
