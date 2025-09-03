module FSharpExtensions

open System.Diagnostics.CodeAnalysis

type SourceName with
    member this.Prop = 1
    member this.Method() = 1

type System.String with
    member this.Prop = 1
    member this.Method() = 1

    [<StringSyntax("regex")>]
    member _.Injection with set (_: string) = ()

type List<'T> with
    member this.Prop = 1
    member this.Method() = 1

type System.Array with
    member this.PropArray = 1
    member this.MethodArray() = 1

type System.Collections.IList with
    member this.PropIList = 1
    member this.MethodIList() = 1

type InjectionsOwner with
    [<StringSyntax("regex")>]
    member this.InjectionExtensionProp with set (_: string) = ()

type System.Threading.Tasks.Task with
    member this.Instance1() = ()
    member this.Instance2(i: int) = ()
    static member Static1() = ()
    static member Static2(i: int) = ()
