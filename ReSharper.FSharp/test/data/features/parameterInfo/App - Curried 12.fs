type I1 = interface end

type I2 =
    inherit I1

[Unchecked.defaultof<I1>] |> List.iter (function :? I2{caret} -> ())
