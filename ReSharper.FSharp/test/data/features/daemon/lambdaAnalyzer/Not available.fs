let f x y = ()

fun () -> ()
fun (()) -> ()
fun () -> id ()

fun x -> -x
fun x -> ~~x

fun x -> x + 1
fun x -> f x 1

fun x y -> x
fun x y -> y x
fun x y -> id x
fun x y -> f y x

fun (a, b) -> f b
fun (a, b, c) -> (c, b, a)
fun (a, b) (c, d, e) -> f (a, b, c, d, e)

fun x -> f x x
fun (a, b) -> f a (a, b)
fun x -> (fun y -> x) x

fun struct(a, b) -> (a, b)
fun (a, b) -> struct(a, b)
fun struct(a, b) -> a
fun struct(a, b) -> b

module A =
    let [<Literal>] b = 5

fun None -> None
fun A.b -> b
fun b -> A.b
fun A.b -> A.b
