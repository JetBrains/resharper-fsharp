module Module

Class.NotNull.Add(fun (e: Args) -> ignore e)
Class.NotNull.Add(fun (e: Args | null) -> ignore e)

Class.Nullable.Add(fun (e: Args) -> ignore e)
Class.Nullable.Add(fun (e: Args | null) -> ignore e)

Class.NotNull.Add(fun e -> e.ToString() |> ignore)
Class.Nullable.Add(fun e -> e.ToString() |> ignore)
