// ${COMPLETE_ITEM:``type`` =}
type T() =
    static member M(``type``: int) = ()

T.M(ty{caret})
