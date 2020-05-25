type IPrintable =
   abstract member Print1 : unit -> unit
   abstract member Print2 : unit -> unit

type SomeClass1(x: int, y: float) =
   interface IPrintable{caret}