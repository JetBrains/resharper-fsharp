type I =
    [<CLIEvent>]
    abstract E: IEvent<int>

type T() =
    interface I{caret}
