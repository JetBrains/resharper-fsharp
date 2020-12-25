do
    {off}

    let x{off} = 1
    let{off} x{off}: int = 1

    let{off} (x{off}: int): int = 1
    let{off} ((x{off}: int)): int = 1
    let{off} ((x{off}: string)): int = 1

    let{on} foo{on} {off}x {off}= {off}()
    (){off}
