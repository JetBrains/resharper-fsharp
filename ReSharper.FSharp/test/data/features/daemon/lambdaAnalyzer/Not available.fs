﻿let f x y = ()
type Delegate = delegate of int -> int
type DelegateAbbreviation = Delegate
type Type1(x: Delegate) = 
    static member M1(x: Delegate) = ()
    static member M2(x: DelegateAbbreviation) = ()
    static member M3(x: int, y: Delegate) = ()

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

Type1(fun x -> x)
Type1.M1(fun x -> x)
Type1.M2(fun x -> x)
Type1.M3(0, fun x -> x)

module A =
    let [<Literal>] b = 5

fun None -> None
fun A.b -> b
fun b -> A.b
fun A.b -> A.b
