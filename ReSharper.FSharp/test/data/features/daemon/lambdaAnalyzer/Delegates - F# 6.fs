type Delegate = delegate of int -> int
type DelegateAbbreviation = Delegate
type Type1(x: Delegate) = 
    static member M1(x: Delegate) = ()
    static member M2(x: DelegateAbbreviation) = ()
    static member M3(x: int, y: Delegate) = ()

    static member M4(x: string) = ()
    static member M4(x: int, y: Delegate) = ()

Type1(fun x -> x)
Type1.M1(fun x -> x)
Type1.M2(fun x -> x)
Type1.M3(0, fun x -> x)
Type1.M3(0, y = fun x -> x)
Type1.M3(0, y = (fun x -> x))
Type1.M4(0, fun x -> x)
