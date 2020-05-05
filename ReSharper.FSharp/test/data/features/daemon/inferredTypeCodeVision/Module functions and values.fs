module Module

let x = 1
let f x = x
let g = fun x -> x + 1
let h f g x y = f x + g y
let i (x, y) z (a, b, c) = x + y + a + string b + c
let j (x, y) = x + y
let k (x, y) z = x + y + z
