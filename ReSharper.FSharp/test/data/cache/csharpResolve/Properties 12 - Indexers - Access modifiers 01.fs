namespace global

type T() =
  member _.Item
    with get (_: int) = 0
    and private set (_: int) (_: int) = ()

  member _.Item
    with internal get (_: string) = 0
    and set (_: string) (_: int) = ()

