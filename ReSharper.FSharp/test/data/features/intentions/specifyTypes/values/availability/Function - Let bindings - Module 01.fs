module Module

{off}

let x1{on} = 1
let{off} x2{off}: int = 1

let{off} (x3{off}: int): int = 1
let{off} ((x4{off}: int)): int = 1

{off}[<CompiledName{off}("Foo")>]{off}
let{off} foo{off} {on}x {on}= {off}()

let{off} f{off} {off}(): unit = ()