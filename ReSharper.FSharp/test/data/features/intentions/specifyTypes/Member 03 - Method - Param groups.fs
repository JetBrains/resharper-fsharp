module Module

type A =
    member x.M{caret} (a, b) c =
        (a ||> String.concat) + b + (c ||> String.concat)
