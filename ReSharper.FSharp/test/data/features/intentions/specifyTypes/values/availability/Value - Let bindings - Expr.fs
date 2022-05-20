do
    {off}

    let x1{on} = 1
    let{off} x2{off}: int = 1

    let{off} (x3{off}: int): int = 1
    let{off} ((x4{off}: int)): int = 1

    let{off} foo{off} {on}x5 {off}= {off}()
    let{off} {on}x6 =
            let{off} {off}y {on}z = 1 + z
            y 1
    let{off} {off}x7<'a>{off} = 1
    (){off}