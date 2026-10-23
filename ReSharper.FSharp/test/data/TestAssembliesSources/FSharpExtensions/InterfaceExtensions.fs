module InterfaceExtensions

type I =
    interface end

type I with
    member _.Prop with set (_: string) = ()

type Class() =
    interface I


open System

type IDisposable with
    member _.Prop with set (_: string) = ()

type Class2() =
    interface IDisposable with
        member this.Dispose() = ()
