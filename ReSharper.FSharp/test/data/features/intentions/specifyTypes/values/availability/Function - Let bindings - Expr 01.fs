do
    {off}

    let x{on} = 1
    let{off} x{off}: int = 1
    let{off} x{on}: _ list = [1]

    let{off} (x{off}: int): int = 1
    let{off} ((x{off}: int)): int = 1

    let{off} foo{off} {on}x {on}= {off}()
    (){off}