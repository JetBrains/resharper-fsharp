[<Extension>]
type Extensions() =
    [<Extension>]
    static member M(t: string, x) = ()

"".M{caret}
