module Module

type NumberPrinter(num) =

    member{on} x{off}.print{on}() = sprintf{off} "%d" num
    member{off} _.print1{off}(): unit = ()
    member{on} _.print2{on}(x): int = x + 1
    member{off} _.print3{off}(x: int): int = x + 1

    member{on} x.P1{on} = 1
    member{off} x.P2{off}: int = 1

    member{off} x.P3{off}
        with get{off}() = num
        and set{off}(y: int) = ()

    member{off} val P4{off} = 3
