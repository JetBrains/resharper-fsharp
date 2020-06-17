module Module

type A(i: int) =
    class end

type B() =
    inherit A

    val F: int

    new (i{caret}: int) =
        { inherit A(i)
          F = i }
