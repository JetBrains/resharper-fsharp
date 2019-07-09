module Module

let x = 1
let f x = x
let g = fun x -> x + 1
let h f g x y = f x + g y
let f (x, y) z (a, b, c) = x + y + a + string b + c
let f (x, y) = x + y
let f (x, y) z = x + y + z
