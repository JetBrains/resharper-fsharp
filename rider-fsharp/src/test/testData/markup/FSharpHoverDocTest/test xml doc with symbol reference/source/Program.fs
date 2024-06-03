/// <exception cref="System.OverflowException">can throw</exception>
let rec fibo<caret>Rec =
  function
  | 0L -> 0L
  | 1L -> 1L
  | n -> fiboRec (n-1L) + fiboRec (n-2L)
