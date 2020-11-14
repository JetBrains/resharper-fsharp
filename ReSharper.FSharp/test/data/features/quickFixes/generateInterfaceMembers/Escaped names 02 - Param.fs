module Module

type I =
  abstract M: ``param 1``: int * ``param 2``: int -> ``type``: int -> unit

type T() =
  interface I{caret}
