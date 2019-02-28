module Module

type T() =
    new(a: int) as this = this{caret} |> ignore T()
