module Module

type I =
    abstract (~~): int -> unit

type T() =
    interface I{caret}
