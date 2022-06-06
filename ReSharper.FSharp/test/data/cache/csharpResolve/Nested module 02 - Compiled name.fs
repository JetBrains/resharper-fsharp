module Top

module Nested =
    let x = 1

type Nested() =
    class end

let f (_: Nested) = ()
