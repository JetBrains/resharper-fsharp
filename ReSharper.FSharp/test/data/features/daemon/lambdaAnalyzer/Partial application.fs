let f x y z = ()

fun x -> f x
fun x -> f 1 x
fun x y -> f y
fun x y -> f 1 y
fun x y -> f 1 x y
fun x -> Math.Abs x

fun x () -> f ()

fun x (a, b) -> f (a, b)
fun x (a, b) -> f 1 (a, b)
fun x (a, b) -> f 1 x (a, b)

fun (a, b) (c, d) -> f 1 (a, b) (c, d)

fun (a, b) x -> f a (a, b) x
