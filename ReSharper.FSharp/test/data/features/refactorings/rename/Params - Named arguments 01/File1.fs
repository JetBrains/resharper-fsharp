module Module

type SpeedingTicket() =
    member this.GetMPHOver(speed: int, limit{caret}: int) = speed - limit
