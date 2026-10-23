module Module

type I =
    interface end

type I with
    member _.Prop with set _ = ()

type Class() =
    interface I

Class(Prop{on} = "x")


open System

type IDisposable with
    member _.Prop with set _ = ()

type Class2() =
    interface IDisposable with
        member this.Dispose() = failwith "todo"


new Class2(Prop{on} = "x")
