type T =
    static member M(a: int) = fun (b: int) -> a + b

{selstart}2 |> T.M(1){selend}
