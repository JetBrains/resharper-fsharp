module Module =
    type Abbr = int

    [<RequireQualifiedAccess{caret}>]  [<CompiledName "E">]
    type E =
        | A = 1
