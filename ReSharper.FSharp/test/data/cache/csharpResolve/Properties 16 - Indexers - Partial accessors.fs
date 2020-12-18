namespace global

type T() =
  member x.Item with get (_: int) = 0
  member x.Item with set (_: string)(_: int) = ()
