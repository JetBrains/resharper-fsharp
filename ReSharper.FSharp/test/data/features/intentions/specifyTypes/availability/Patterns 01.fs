module Module

let x{off} = 1
let a, b{on} = 1, 1

let f{off} x{on} (So{off}me(y{on})) (z{off}: int) = ()

type A() =
  member _.M(?x{on}, ?y{off}: int) = x + y
