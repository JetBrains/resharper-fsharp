module Test

module A = 
 let f _ = ()

let _ = fun x -> A.f x
