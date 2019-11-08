module Module

[<Foo>]
let x{on} = 1

[<Foo>]
let foo{on} {off}x = ()

let [<Foo>] x{on} = 1
let [<Foo>] foo{on} {off}x = ()
