open System

type A() =
    static member M1(x: int -> int) = ()
    static member M2(x: int -> int) = ()
    static member M2(x: string -> string) = ()
    static member M3(x: Func<int, int>) = ()
    static member G1(x: int) = x

let f (x: int -> int) = ()
let f2 (x: int) (y: int -> int) = ()
let g (x: int) = x

(fun x -> x) |> f
f <| (fun x -> x)
(fun x -> g x) |> f
f <| fun x -> g x
(fun x -> g x) |> f2 1
(1, fun x -> g x) ||> f2
(fun x -> g x) |> A.M1
A.M1 <| fun x -> g x
(fun x -> A.G1 x) |> A.M1
(fun x -> g x) |> A.M2
(fun x -> A.G1 x) |> A.M2
(fun x -> g x) |> A.M3
