module Module

type T() = 
    [<CLIEvent>]
    member this.Event = Event<int>().Publish
