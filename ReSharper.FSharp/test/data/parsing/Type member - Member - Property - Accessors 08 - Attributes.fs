type A() =
    [<method: M>]
    member this.Index with [<method: M>] get () = 5
                       and [<method: M>] set () = ()
