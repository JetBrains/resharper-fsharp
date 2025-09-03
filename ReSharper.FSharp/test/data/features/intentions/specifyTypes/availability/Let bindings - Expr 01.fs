do
    {off}

    let{on} x{on} = 1
    let{off} x{off}: int = 1

    let{on} (x{on}) = 1
    let{off} (x{off}): int = 1
    let{off} (x{off}: int) = 1
    let{off} (x{off}: int): int = 1
    let{off} ((x{off}: int)): int = 1

    let{on} rec{off} foo{on} {off}x {off}= {off}()
    (){off}
