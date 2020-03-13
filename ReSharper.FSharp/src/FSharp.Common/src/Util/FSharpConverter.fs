[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter

open System
open FSharp.Compiler.AbstractIL.IL
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
open FSharp.Compiler.ExtensionTyping
open FSharp.Compiler.AbstractIL.Internal.Library
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol

type JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdVersion with
    [<Extension; CompiledName("ToSystemVersion")>]
    member this.toSystemVersion() =
        Version(this.Major, this.Minor, this.Build, this.Revision)

type Version with
    [<Extension>]
    member this.toRdVersion() =
        RdVersion(this.Major |> int,
                  this.Minor |> int,
                  this.Build |> int,
                  this.Revision |> int)

type ILVersionInfo with
    [<Extension>]
    member this.toRdVersion() =
        RdVersion(this.Major |> int,
                  this.Minor |> int,
                  this.Build |> int,
                  this.Revision |> int)

type ResolutionEnvironment with
    [<Extension>]
    member this.toRdResolutionEnvironment() =
        RdResolutionEnvironment(this.resolutionFolder,
                                Option.toObj this.outputFile,
                                this.showResolutionMessages,
                                this.referencedAssemblies,
                                this.temporaryFolder)
      
let bindAll = BindingFlags.DeclaredOnly ||| BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static ||| BindingFlags.Instance

type System.Object with

           member x.GetProperty(nm) =
               let ty = x.GetType()
               let prop = ty.GetProperty(nm, bindAll)
               let v = prop.GetValue(x,null)
               v

           member x.GetField(nm) =
               let ty = x.GetType()
               let fld = ty.GetField(nm, bindAll)
               let v = fld.GetValue(x)
               v

           member x.HasProperty(nm) =
               let ty = x.GetType()
               let p = ty.GetProperty(nm, bindAll)
               p |> isNull |> not

           member x.HasField(nm) =
               let ty = x.GetType()
               let fld = ty.GetField(nm, bindAll)
               fld |> isNull |> not
               
           member x.GetElements() = [ for t in (x :?> System.Collections.IEnumerable) -> t ]