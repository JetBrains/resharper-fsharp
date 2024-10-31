module Module

do
    let rec f1 x =
        f1 (x + 1)

    let rec f2 x =
        f2 (x + 1)
        ()

    let rec f3 x =
        f3
        ()

    let rec f4 a b =
        f4 a
        ()

    let rec f5 a b =
        f5 a

    let rec f6 a b =
        f6


    ()
