type A(x, ?y) =
    member val Prop = 1 with get, set

{selstart}A(1, Prop = 2, what = 4, y = 3){selend}
