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
        
type PublicKey with
    [<Extension>]
    member this.toRdPublicKey() =
        RdPublicKey(this.IsKey,
                    this.IsKeyToken,
                    this.Key,
                    this.KeyToken)

type ILModuleRef with
    [<Extension>]
    member this.toRdILModuleRef() =
        RdILModuleRef(this.Name,
                      this.HasMetadata,
                      Option.toObj this.Hash)

type ILAssemblyRef with
    [<Extension>]
    member this.toRdILAssemblyRef() =
        RdILAssemblyRef(this.Name,
                        this.QualifiedName,
                        Option.toObj this.Hash,
                        (match this.PublicKey with | Some x -> x.toRdPublicKey() | _ -> null),
                        this.Retargetable,
                        (match this.Version with | Some x -> x.toRdVersion() | _ -> null),
                        Option.toObj this.Locale)

type ILScopeRef with
    [<Extension>]
    member this.toRdILScopeRef() =
        (*RdILScopeRef(this.IsModuleRef,
                     this.IsAssemblyRef,
                     (if this.IsModuleRef then this.ModuleRef.toRdILModuleRef() else null),
                     (if this.IsAssemblyRef then this.AssemblyRef.toRdILAssemblyRef() else null),
                     this.QualifiedName) *)
        null
        
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