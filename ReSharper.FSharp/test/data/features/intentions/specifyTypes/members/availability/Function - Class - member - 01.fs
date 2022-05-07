module Module

type NumberPrinter(num) =
    member x.print{on}() = sprintf{on} "%d" num
    member x.prop{on} = num