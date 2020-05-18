module M =
    let f a z =
        let x, y = a
        x + y + z

{selstart}M.f (1, 2) 3{selend}
