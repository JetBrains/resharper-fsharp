namespace N1.N

module A1 =
    module A2 =
        let x = 5
        let f x = 1
        let (|One|_|) (x: int) = Some(1)
        type T2<'t, 'm>() =
            inherit ResizeArray<int>()
            let x = 4
            new (x: int) = T2()
            member _.M2 x (y: 'a when 'a :> System.IDisposable, z) = x(y) + z
            member _.M3 (x, ?y) = y.Value + 1
            member val Prop = 5 with get, set
            interface System.IDisposable with
                member this.Dispose() = ()

        [<Interface>]
        type Interface =
            inherit System.IDisposable
            abstract member M: x: int -> unit

    exception E1 of string
        with member x.NewMessage = ""

    type Record = {
        Field1: int
        Field2: int
    }

    type DU =
        | Case1 of int
        | Case2

    type Struct =
        struct
            val X: float
            val Y: float
            new(x: float, y: float) = { X = x; Y = y }
        end

    type System.Collections.Generic.List<'a> with
        member x.Length = x.Count

namespace N2.N