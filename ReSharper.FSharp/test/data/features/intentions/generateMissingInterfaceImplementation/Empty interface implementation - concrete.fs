type IPrintable =
   abstract member Print1 : unit -> unit
   abstract member Print2 : string -> unit
   // TODO: Use multiple variables here, not tupled
   // abstract member Print3 : string:namedStringInTuple * int -> unit
   // abstract member Print4 : string * int:namedIntInTuple -> namedInt:int -> unit
   abstract member Print3 : (string * int) -> unit
   abstract member Print4 : (string * int) -> namedInt:int -> unit

type SomeClass1(x: int, y: float) =
   interface IPrintable{caret}