module Module

type IA<'T> =
    abstract M2: 'T -> unit
    abstract M1: 'T -> unit

type IB =
    inherit IA<int>
    inherit IA<string>

let o =
    { new obj() with
        override this.ToString() = ""

      interface IB{caret} }
