type A(_a: int, ?_b: int) =
    static member M(x, y) = ()
    static member M1(x, ?y) = ()

type A1(_a: int, _b: int) = class end
type C(?_a: int) = class end
type C1(_a: int) = class end


[(1, 1)] |> List.map (fun (a, b) -> A(a, b))
let _ = fun (a, b) -> A(a, b)

[(1, 1)] |> List.map (fun (a, b) -> A.M(a, b))
let _ = fun (a, b) -> A.M(a, b)

[(1, 1)] |> List.map (fun (a, b) -> A.M1(a, b))
let _ = fun (a, b) -> A.M1(a, b)

[(1, 1)] |> List.map (fun (a, b) (c, d) -> A.M1(c, d))
let _ = fun (a, b) (c, d) -> A.M1(c, d)

[(1, 1)] |> List.map (fun (a, b) -> A1(a, b))
let _ = fun (a, b) -> A1(a, b)

[1] |> List.map (fun a -> C(a))
let _ = fun a -> C(a)

[1] |> List.map (fun a -> C1(a))
let _ = fun a -> C1(a)
