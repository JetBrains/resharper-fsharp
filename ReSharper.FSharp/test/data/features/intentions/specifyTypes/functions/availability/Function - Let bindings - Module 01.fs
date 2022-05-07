module Module

{off}

let x1{off} = 1
let{off} x2{off}: int = 1

let{off} (x3{off}: int): int = 1
let{off} ((x4{off}: int)): int = 1

{off}[<CompiledName{off}("Foo")>]{off}
let{on} foo{on} {off}x {off}= {off}()

let{off} f{off} {off}(): unit = ()