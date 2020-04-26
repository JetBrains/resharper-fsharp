module Module

[<Foo>]
let x{on} = 1

[<Foo>]
let foo{on} {on}x = ()

let [<Foo>] x{on} = 1
let [<Foo>] foo{on} {on}x = ()
