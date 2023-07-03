module Module

type Meh() =
    member _.Bind _ = ()
    member _.Bind2 _ = ()
    member _.Zero () = ()

let meh = Meh()

meh {
    let! x{off} = async { return 1 }
    and! y{off} = async { return 2 }
    ()
}
