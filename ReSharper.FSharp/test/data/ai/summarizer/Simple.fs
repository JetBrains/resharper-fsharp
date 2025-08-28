namespace N1

module A1 =
    module A2 =
        let x = 5
        let f x = 1
        type T2() =
            inherit ResizeArray<int>()
            let x = 4
            new (x: int) = T2()
            member _.M2 x (y, z) = x(y) + z
            interface System.IDisposable with
                member this.Dispose() = ()

    exception E1 of string
        with member x.NewMessage = ""

namespace N2