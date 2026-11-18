module Module

type IA<'T> =
    abstract M1: 'T -> unit
    abstract M2: 'T -> unit

let f (x: 'T) =
    { new obj() with
        override this.ToString() = ""

      interface IA{caret}<'T> with
          member this.M2 _ = () }
