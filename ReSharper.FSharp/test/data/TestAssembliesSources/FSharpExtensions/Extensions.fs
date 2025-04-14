module FSharpExtensions

type SourceName with
    member this.Prop = 1
    member this.Method() = 1

type System.String with
    member this.Prop = 1
    member this.Method() = 1

type List<'T> with
    member this.Prop = 1
    member this.Method() = 1

type System.Array with
    member this.PropArray = 1
    member this.MethodArray() = 1

type System.Collections.IList with
    member this.PropIList = 1
    member this.MethodIList() = 1
