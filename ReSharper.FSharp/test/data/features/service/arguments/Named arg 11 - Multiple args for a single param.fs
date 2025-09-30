type A =
    static member M(a: int, ?b: int) = ()

{selstart}A.M(1, 1, b = 1){selend}
