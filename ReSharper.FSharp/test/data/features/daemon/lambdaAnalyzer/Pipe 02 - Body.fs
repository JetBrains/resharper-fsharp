let f x y z = ()
let add x = x + 1
let t = 1, 2

fun x -> x |> add
fun x -> add <| x
fun x -> x |> f 1 2
fun x -> f 1 2 <| x
fun x y -> y |> f 1 x
fun x y -> (x, y) ||> f 1
fun x y -> f 1 <|| (x, y)
fun x y -> y |> f x 1

fun t -> t ||> f 1
fun x -> x |> f x 1
fun x -> 1 |> add
