type IPrintable =
  abstract member Print1 : unit -> unit
  abstract member Print2 : string -> unit
  abstract member Print3 : namedString:string * namedInt:int -> unit
  abstract member Print4 : string * int -> namedInt:int -> unit

type SomeClass1(x: int, y: float) =
  interface IPrintable{caret}