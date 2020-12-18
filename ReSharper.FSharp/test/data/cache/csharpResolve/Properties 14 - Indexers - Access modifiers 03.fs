namespace global

type T() =
  member _.Item
    with private get (_: int)= 0
    and internal set (_: int) (_: int) = ()
