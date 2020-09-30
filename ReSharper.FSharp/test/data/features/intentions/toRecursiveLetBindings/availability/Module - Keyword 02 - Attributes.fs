module Module

[<Foo>]
let{off} x = 1

[<Foo>]
let{on} foo x = ()

let{off} [<Foo>] x = 1
let{on} [<Foo>] foo x = ()
