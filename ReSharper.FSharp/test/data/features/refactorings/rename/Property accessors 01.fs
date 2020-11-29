namespace global

type T =
  static member A
      with get (_: int) = 0
      and set (_: int) (_: int) = ()

  static member A with get (_: int, _: int) = 0
  static member A with set (_: int, _: int) (_: int) = T.A(0) <- 0

  static member A with get (_: int, _: int, _: int) = 0

  static member A with set (_: int, _: int, _: int, _: int) (_: int) = T.A(0, 0) <- 0
