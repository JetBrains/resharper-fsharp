namespace global

type T() =
  member _.A
    with get (_: int) = 0
    and set (_: int) (_: int) = ()

  member _.A with get t = 0
  member _.A with get (_: int, _: int) = 0
  member _.A with set (_: int, _: int) (_: int) = ()

  member _.A with get (_: int, _: int, _: int) = 0

  member _.A with set (_: int, _: int, _: int, _: int) (_: int) = ()
