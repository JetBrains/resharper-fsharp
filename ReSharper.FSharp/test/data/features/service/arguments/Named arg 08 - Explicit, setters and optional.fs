type A(x, ?y, ?z) =
    member val Prop1 = 1 with get, set
    member val Prop2 = 1 with get, set

{selstart}A(1, Prop1 = 2, y = 1, Prop2 = 1, z = 1){selend}
