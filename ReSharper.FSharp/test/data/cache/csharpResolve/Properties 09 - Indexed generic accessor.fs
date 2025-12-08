namespace global

type T<'a>() =
  let s: 'a [] = [||]

  member _.A 
    with get (_: int) = s
    and set (_: int) (v: 'a) = ()
