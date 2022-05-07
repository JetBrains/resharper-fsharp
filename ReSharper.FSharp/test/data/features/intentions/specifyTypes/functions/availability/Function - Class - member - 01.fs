module Module

type NumberPrinter(num) =
    // Class members aren't supported yet
    member x.print{off}() = sprintf{off} "%d" num