namespace global

type T =
  member x.A
    with get (_: int) = x.get_A{caret}(0)
    and set (_: int) (_: int) = x.set_A(0, 0)
