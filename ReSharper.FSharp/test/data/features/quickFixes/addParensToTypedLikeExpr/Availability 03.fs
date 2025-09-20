module Test

type A() = class end
type [<Sealed>] B() = inherit A()
  
let a = A()

let _ = not a :> B
