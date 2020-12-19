namespace global

type T() =
  member _.A
    with get () = 0
    and set (_: int) = ()

  member _.B 
    with get (()) = 0
    and set (a, b) = ()

  member _.C with set ((a, b)) = ()
