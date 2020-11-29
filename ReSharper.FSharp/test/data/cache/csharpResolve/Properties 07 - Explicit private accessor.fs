namespace global

type T() =
  member _.A 
    with get (_: int) = 0
    and private set (_: int) (v: int) = ()
