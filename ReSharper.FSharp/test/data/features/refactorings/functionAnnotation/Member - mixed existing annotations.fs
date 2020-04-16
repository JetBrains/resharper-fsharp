module Module

open System

type NumberStringProducer() =
    member this.PrintWithFormat{caret} first (second : int32) =
        sprintf "First: %d Second: %d" first second