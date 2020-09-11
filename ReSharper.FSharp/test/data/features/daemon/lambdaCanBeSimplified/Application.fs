let f a b = ()

fun x -> id x
fun x -> (fun x -> 5) x
fun (a, b) c -> f (a, b) c
fun (a, b) (c, d, e) -> f (a, b) (c, d, e)
