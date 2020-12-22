module Module

type T =
  member x.P 
    with private get _ = () 
    and private set _ = ()
