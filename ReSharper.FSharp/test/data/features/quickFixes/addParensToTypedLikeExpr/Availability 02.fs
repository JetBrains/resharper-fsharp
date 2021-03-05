module Test

type A() =
  class end

[<Sealed>]
type B() =
  inherit A()
  
let a = A()
let b = B()

let _ = not a :? B
let _ = not a :> B
let _ = not a :?> B

let _ = not b :? A
let _ = not b :> A
let _ = not b :?> A
