module Test{off} =

    type X{off}() =
        member _.X{on}(x{off}){off} = ()
        member _.X2{on} = ()

    let f{off} x{off} y{off} = ()