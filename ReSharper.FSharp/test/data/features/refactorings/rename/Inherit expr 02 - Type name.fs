module Module

type A(i: int) =
    class end

type B() =
    inherit A

    val F: int

    new (i: int) =
        { inherit A{caret}(i)
          F = i }
