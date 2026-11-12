module Module

type I =
    abstract P2: int
    [<CLIEvent>]
    abstract E: IEvent<System.EventHandler, System.EventArgs>
    abstract M: unit -> unit
    abstract P1: int

type T() =
    interface I{caret}
