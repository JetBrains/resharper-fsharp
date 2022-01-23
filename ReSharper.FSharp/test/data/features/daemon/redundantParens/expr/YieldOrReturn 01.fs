module M

seq {
    yield (1)
    yield (id 1)
    yield (1 |> id)

    yield (if true then 1 else 2)
    yield (match () with _ -> 1)
    yield (try 1 with _ -> 2)

    yield (1 :> int)
    yield (1:? int)

    yield (1; 2)
    yield (1: int)
}

yield (1)
yield (id 1)
