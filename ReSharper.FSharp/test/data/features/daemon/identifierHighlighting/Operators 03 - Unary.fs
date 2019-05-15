module Module

type T() =
    static member (~-) (t: T) = t
    static member (~~) (t: T) = t
    static member (~~~) (t: T) = t

let t = T()
let t1 = -t
let t2 = ~~t
let t3 = ~~~t
