namespace global

type T() =
  member _.A
    with get () = 0
    and set (_: int) = ()
