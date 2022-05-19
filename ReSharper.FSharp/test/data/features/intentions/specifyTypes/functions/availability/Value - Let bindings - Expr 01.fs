do
    {off}

    let x{off} = 1
    let{off} x{off}: int = 1

    let{off} (x{off}: int): int = 1
    let{off} ((x{off}: int)): int = 1

    let{on} foo{on} {off}x {off}= {off}()
    let{off} {off}x =
        let{on} {on}y {off}z = 1 + z
        y 1
    let{on} {on}x<'a>{on} = 1
    (){off}