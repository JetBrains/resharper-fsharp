module Module

type System.String with
    member this.M{caret}(a) = a ||> String.concat
