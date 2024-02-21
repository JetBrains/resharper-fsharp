module Module

type I<'a> =
    abstract P: int
    abstract M: 'a -> unit
    abstract M: string -> unit

{ new obj() with
    override this.ToString() = ""

  interface I{caret}<int> with
      member this.M(s: string) = () }
