module Module

[<AbstractClass>]
type T() =
    abstract M: unit -> string

{ new {caret}T() with }
