type T() =
    static member M(u: unit) = ()

T.M({selstart}u = (){selend}{caret})
