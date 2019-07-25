module Module

type IFSharpInterface = 
    [<CLIEvent>]
    abstract TheEvent: IEvent<exn>
