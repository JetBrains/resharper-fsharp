do
    let f1{on} (a, b): int = a + b
    let f2{on} (a: int, (b: int, c: int)): int = 1
    let f3{on} (a: int, b as c): int = 1

    let g1{off} (a: int, (b, c): int * int): int = 1
    let g2{off} (a: int, b: int as c): int = 1
    let g3{off} (a, b as c: int * int): int = 1

    ()
