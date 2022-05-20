module Module

type NumberPrinter(num) =
    member x.print{on}() = sprintf{off} "%d" num
    member x.prop{on} = num
    member x.print2{on}(num2) = sprintf{off} "%d" (num + num2)
    member x.prop2{off}: int = num
    member x.print3{on}(num2: int) = sprintf{off} "%d" num
    member x.print4{off}(num2: int): string = sprintf{off} "%d" num