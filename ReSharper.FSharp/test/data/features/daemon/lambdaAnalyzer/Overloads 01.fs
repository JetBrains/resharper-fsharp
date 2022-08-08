open System
open System.Runtime.CompilerServices

type A =
    static member M(x: int) = x

type B =
    static member B(x: int -> int) = ()

B.B(fun x -> A.M x)
B.B(A.M) //OK
B.B(fun x -> x)
B.B(id) //OK


type A1 =
    static member M(x: int) = x

type B1 =
    static member B(x: Func<int, int>) = ()

B1.B(fun x -> A1.M x)
B1.B(A1.M) //ERROR
B1.B(fun x -> x)
B1.B(id) //OK


type A2 =
    static member M(x: int) = x

type B2 =
    static member B(x: Func<int, int>) = ()
    static member B(x: string) = ()

B2.B(fun x -> A2.M x) //FALSE NEGATIVE
B2.B(A2.M) //OK
B2.B(fun x -> x)
B2.B(id) //OK


type A3 =
    static member M(x: int) = x
    static member M(x: string) = x

type B3 =
    static member B(x: int -> int) = ()

B3.B(fun x -> A3.M x)
B3.B(A3.M) //OK
B3.B(fun x -> x)
B3.B(id) //OK


type A4 =
    static member M(x: int) = x
    static member M(x: string) = x

type B4 =
    static member B(x: Func<int, int>) = ()

B4.B(fun x -> A4.M x)
B4.B(A4.M) //ERROR
B4.B(fun x -> x)
B4.B(id) //OK


type A5 =
    static member M(x: int) = x
    static member M(x: string) = x

type B5() =
    member _.B(x: int -> int) = ()
    member _.B1(x: int -> int) = ()

[<Extension>]
type B5Ext =
    [<Extension>]
    static member B(this: B5, x: string) = ()
    [<Extension>]
    static member B1(this: B5, x: string) = ()

B5().B(fun x -> A5.M x)
B5().B(A5.M) //ERROR
B5().B1(fun x -> A5.M x)
B5().B1(A5.M) //ERROR
B5().B(fun x -> x)
B5().B(id) //OK

