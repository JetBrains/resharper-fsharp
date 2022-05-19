do
    {off}

    let x{on} = 1
    let{off} x{off}: int = 1

    let{off} (x{off}: int): int = 1
    let{off} ((x{off}: int)): int = 1

    let{off} foo{off} {on}x {off}= {off}()
    let{off} {on}x =
            let{off} {off}y {on}z = 1 + z
            y 1
    let{off} {off}x<'a>{off} = 1
    (){off}