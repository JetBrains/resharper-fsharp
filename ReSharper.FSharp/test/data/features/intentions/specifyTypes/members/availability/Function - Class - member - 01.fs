module Module

type NumberPrinter(num) =
    member x.m1{on}() = sprintf{off} "%d" num
    member x.m2{on}(num2) = sprintf{off} "%d" (num + num2)
    member x.m3{on}(num2: int) = sprintf{off} "%d" num
    member x.m4{off}(num2: int): string = sprintf{off} "%d" num

    member x.p1{on} = num
    member x.p2{off}: int = num
    member x.p3{on}: _ list = [num]

    // TODO: fix these
    member val v1{off} = num // should be on
    member val v2{off}: int = num
    member val v3{off}: _ list = [num] // should be on