let f a b = ()

fun x () -> f x ()
fun () () -> f () ()

fun x -> id x
fun x -> (fun x -> 5) x

fun (a, b) c -> f (a, b) c
fun (a, b) (c, d, e) -> f (a, b) (c, d, e)

fun x -> "".Foo.Bar(x)
fun x -> System.Math.Abs(x)
fun x -> List<int>.Equals(x)
