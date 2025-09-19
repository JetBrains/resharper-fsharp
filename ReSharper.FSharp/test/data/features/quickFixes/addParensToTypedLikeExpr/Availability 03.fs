module Test

type A() =
  class end

[<Sealed>]
type B() =
  inherit A()
  
let a = A()

let _ = not a :> B
