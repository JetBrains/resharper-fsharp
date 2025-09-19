module Test

type A() =
  class end

[<Sealed>]
type B() =
  inherit A()

let b = B()

let _ = not b :> A
