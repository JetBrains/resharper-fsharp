module Module

type IEnumerableExtensions =
    [<Extension>]
    static member M{caret}(xs: IEnumerable<string>, a) =
        Seq.contains (a ||> String.concat) xs 
