module Module

type AsyncBuilder with
    member x.Bind2(x1: Async<'T1>, x2: Async<'T2>, f: 'T1 * 'T2 -> Async<'T3>) = failwith ""

async {
    let! x = async { return 1 }
    and! y{caret} = async { return "" }
    ()
}
