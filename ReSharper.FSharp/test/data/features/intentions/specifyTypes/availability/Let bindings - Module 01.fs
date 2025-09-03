module Module

{off}

let x1{on} = 1
let{off} x2{off}: int = 1

let{off} (x3{off}: int): int = 1
let{off} ((x4{off}: int)): int = 1

{off}[<CompiledName{off}("Foo")>]{off}
let{on} foo{on} {off}x {off}= {off}()

let{off} rec f{off} {off}(): unit = ()
and{on} g{on} x = x + 1

let{on} f1{on} ([<Attr>] x): int = x + 1
let{on} f2{on} (([<Attr>] (x))): int = x + 1
let{off} f3{off} ([<Attr>] x: int): int = x + 1
let{off} f4{off} ([<Attr>] (x: int)): int = x + 1
let{off} f5{off} (([<Attr>] (x: int))): int = x + 1
