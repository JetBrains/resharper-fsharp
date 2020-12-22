namespace global

type T1() =
    [<CompiledName "item">]
    member x.Item with get(_: int) = 1

type T2() =
    [<CompiledName "Item">]
    member x.item with get(_: int) = 1

type T3() =
    [<CompiledName "Item">]
    member x.Item with get(_: int) = 1
