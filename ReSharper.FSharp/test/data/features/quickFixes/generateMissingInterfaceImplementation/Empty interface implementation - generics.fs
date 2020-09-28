type IPrintable<'a> =
   abstract member Print1 : unit -> unit
   abstract member Print2 : 'a -> unit
   abstract member Print3 : ('a * int) -> unit
   abstract member Print4<'b,'c> : ('c * 'b) -> namedInt:int -> unit

type SomeClass1(y: float) =
   interface IPrintable<float>{caret}