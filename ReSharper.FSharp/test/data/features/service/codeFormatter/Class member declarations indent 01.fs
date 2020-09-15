module Module

type B() = class end

type A(a) =
  inherit B()

  let a = ()

  do ()

  new () = A(3)

  member x.B() = ()

  abstract member C : unit -> unit
  default x.C() = ()

  member val D = ()
