type T =
    static member M(a: int, b: int) = a + b

{selstart}(1, 2) |> T.M{selend}
