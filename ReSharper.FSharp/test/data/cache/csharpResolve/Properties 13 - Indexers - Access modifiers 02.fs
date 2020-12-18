namespace global

type T() =
  member private _.Item
    with get (_: int)= 0
    and set (_: int) (_: int) = ()
