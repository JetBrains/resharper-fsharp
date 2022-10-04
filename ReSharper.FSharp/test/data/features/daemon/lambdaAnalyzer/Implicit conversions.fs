module Test

type I =
    interface
    end

type T(x) =
    interface I
    static member F(x) = T(x)
    static member F1(x) = T(x) :> I
    static member Id(x) = x

let f x = T(x)
let g x : I = T(x)

let _: _ -> I = fun x -> f x
let _: _ -> I = fun x -> g x
let _: _ -> I = fun x -> id x
let _: _ -> I = fun x -> T x
let _: _ -> I = fun x -> T.F(x)
let _: _ -> I = fun x -> T.F1(x)
let _: _ -> I = fun x -> T.Id(x)
let _: I[] = [||] |> Array.map (fun x -> T(x))
let _: T[] = [||] |> Array.map (fun x -> T(x))
let _ = if true then (id: _ -> I) else fun x -> T(x)
let _ = if true then (id: _ -> I) else fun x -> T.Id(x)

let h x y : I = T(x)

let _: _ -> _ -> I = fun x y -> h x y


let _: int -> float = fun x -> x
let _: string = fun x -> x


let f1 x y = 5
let _: int -> FSharpFunc<int, int> = fun x -> f1 x
