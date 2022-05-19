module Module

type NumberPrinter(num) =
    member x.print{on}() = sprintf{off} "%d" num
    member x.prop{on} = num