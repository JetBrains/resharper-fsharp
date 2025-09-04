namespace global

type GlobalType = class end

namespace N1.N

[<AutoOpen>]
module M1 =
    let x = 1
    let f x = x
    let (+) a b = a + b
    let (|One|_|) x = Some(x)
    let (Some(some)) = Some(1)

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

    [<Interface>]
    type IInterface =
        inherit System.IDisposable
        inherit System.Collections.Generic.IList<string>
        abstract member M: x: int -> unit

    type System.Collections.Generic.List<'a> with
        member x.Length = x.Count

    type GenericType<'t>(x: string) =
        inherit ResizeArray<int>()
        let x = 4
        static let staticX = 4

        new (x: 't) = new GenericType<'t>(string x)

        member _.Method1 x (y: 'a when 'a :> System.IDisposable, z) = x(y) + z
        member _.Method2 (x, ?y) = y.Value + 1
        member val Prop1 = 5 with get
        member _.Prop2 with get (x: string) = 1 and set (x: string) (z: int) = ()

        interface System.IDisposable with
            member this.Dispose() = ()

        interface System.Collections.Generic.IList<string> with
            member this.Add(item) = failwith "todo"

    module NestedModule =
        let x = 5

namespace N2.N