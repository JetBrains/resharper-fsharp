type T() =
    static member M(u: unit) = ()

let u = ()
{selstart}T.M(u){selend}
