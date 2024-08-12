// ${COMPLETE_ITEM:b}
module Module

type A =
    { a: string }

type B =
    { b: A }

let x = { b = { a = "hello" } }

let y =
    { x with b{caret} }