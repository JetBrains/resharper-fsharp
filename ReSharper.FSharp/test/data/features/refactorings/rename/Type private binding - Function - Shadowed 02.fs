module Module

type T() =
    let f () = ()
    let f () = f{caret} ()

    do f ()
