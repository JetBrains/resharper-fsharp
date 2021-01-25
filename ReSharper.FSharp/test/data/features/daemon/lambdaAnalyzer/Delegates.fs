type Delegate = delegate of int -> int
type DelegateAbbreviation = Delegate

let f (x: Delegate) = ()

type A() = 
    static member M1(x: int -> int) = ()
    static member M2(x: int, y: int -> int) = ()
    static member M3(x: int, y: (int * int -> int)) = ()
    static member M4(x: Unresolved) = ()

f (fun x -> x)
Delegate(fun x -> x)
(fun (x: Delegate) -> ()) (fun x -> x)
A.M1(fun x -> x)
A.M1(0, fun x -> x)
A.M2(0, fun x -> x)
A.M3(0, (0, fun x -> x))
A.M4(fun x -> x)
A.UnresolvedM(fun x -> x)
