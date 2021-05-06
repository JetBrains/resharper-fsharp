module Module

type T() =
    let f () = ()
    let f () = f ()

    do f{caret} ()
