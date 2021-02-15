module Test

module A =
    let f _ _ = ()

let _ = fun x -> A.f 1 x
