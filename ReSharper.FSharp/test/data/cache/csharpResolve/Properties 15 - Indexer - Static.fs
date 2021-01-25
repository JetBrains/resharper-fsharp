namespace global

type T() =
  static member Item
    with get (_: int) = 0
    and set (_: int) (_: int) = ()
