let f x y z = ()
let add x = x + 1

fun x y -> x |> f 1 <| y
fun x y -> y |> (x |> f 1)
fun x -> x |> (fun y -> y + 1)
fun (a, b) -> (a, b) |> fst
fun x -> x |> Some
fun x -> (x) |> add
fun x -> (x |> add)

fun x -> x |> add |> add
fun (x: int) -> x |> add
fun x y -> (y, x) ||> f 1
fun x -> x |> f (add 1) 2
fun x -> x |> add <| 1
