let f x y = ()

fun x -> x + 1
fun x -> f x 1

fun x y -> x
fun x y -> y x
fun x y -> id x
fun x y -> f y x

fun (a, b) -> f b
fun (a, b, c) -> (c, b, a)
fun (a, b) (c, d, e) -> f (a, b, c, d, e)
