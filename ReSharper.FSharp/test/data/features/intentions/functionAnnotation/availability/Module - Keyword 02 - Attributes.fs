module Module

[<Foo>]
let{on} x = 1

[<Foo>]
let{on} foo x = ()

let{on} [<Foo>] x = 1
let{on} [<Foo>] foo x = ()
