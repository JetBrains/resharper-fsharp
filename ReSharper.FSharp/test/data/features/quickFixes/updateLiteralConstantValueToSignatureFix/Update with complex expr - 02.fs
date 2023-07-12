module A

[<Literal>]
let a = 123

[<Literal>]
let c{caret} = 23 + 42 + a
