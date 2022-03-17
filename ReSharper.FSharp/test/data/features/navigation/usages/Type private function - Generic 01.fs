module Module

type T<'A, 'B>() =
    let a: 'A = Unchecked.defaultof<_>
    let f{on} x = a, x
    
    do f ""
