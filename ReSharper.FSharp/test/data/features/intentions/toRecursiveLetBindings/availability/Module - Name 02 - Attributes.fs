module Module

[<Foo>]
let x{off} = 1

[<Foo>]
let foo{on} {off}x = ()

let [<Foo>] x{off} = 1
let [<Foo>] foo{on} {off}x = ()
