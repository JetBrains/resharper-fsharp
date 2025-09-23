module Test

type A() = class end
type [<Sealed>] B() = inherit A()

let b = B()

let _ = not b :?> A
