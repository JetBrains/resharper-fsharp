namespace ModuleMembers

module Say =
    let x = 1
    let [<Literal>] Literal = 1

    let f x = ()

    let (|Aaa|_|) _ = Some()

    let (|Bbb|Ccc|) x = if x then Bbb else Ccc

    type U =
        | Case1
        | Case2 of named: int * int
