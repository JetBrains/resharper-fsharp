module Module

let (_, _) as a = 1, 2
let ((_, _) as a) = 1, 2

let _, ((_, _) as a) = 1, (2, 3)
let (_, ((_, _) as a)) = 1, (2, 3)

let ((_, _) as a), _ = (1, 2), 3
let (((_, _) as a), _) = (1, 2), 3
