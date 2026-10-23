module Test

open System.Diagnostics.CodeAnalysis

type I =
    interface end

type I with
    [<StringSyntax("regex")>]
    member _.Prop with set (_: string) = ()

type Class() =
    interface I

let _ = Class(Prop = "[123]")
