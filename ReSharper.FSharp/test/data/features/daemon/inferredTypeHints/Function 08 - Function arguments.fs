let call f x =
    let y = f (12, (15, [1; 2; 3])) * 15.
    x f
