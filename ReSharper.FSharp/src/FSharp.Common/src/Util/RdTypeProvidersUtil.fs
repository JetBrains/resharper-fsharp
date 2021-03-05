[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter

open FSharp.Compiler.AbstractIL.IL
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
open FSharp.Compiler.ExtensionTyping

type ILVersionInfo with
    [<Extension>]
    member this.toRdVersion() =
        RdVersion(this.Major |> int,
                  this.Minor |> int,
                  this.Build |> int,
                  this.Revision |> int)

type JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdVersion with
    [<Extension>]
    member this.toVersion() =
        ILVersionInfo(this.Major |> uint16,
                      this.Minor |> uint16,
                      this.Build |> uint16,
                      this.Revision |> uint16)

type JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdPublicKey with
    [<Extension>]
    member this.toPublicKey() =
        if this.IsKey then PublicKey this.Data else
        PublicKeyToken this.Data

type PublicKey with
    [<Extension>]
    member this.toRdPublicKey() =
        match this with
        | PublicKey key -> RdPublicKey(true, key)
        | PublicKeyToken token -> RdPublicKey(false, token) 

type ResolutionEnvironment with
    [<Extension>]
    member this.toRdResolutionEnvironment() =
        RdResolutionEnvironment(this.resolutionFolder,
                                Option.toObj this.outputFile,
                                this.showResolutionMessages,
                                this.referencedAssemblies,
                                this.temporaryFolder)

type JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdResolutionEnvironment with
    [<Extension; CompiledName("ToResolutionEnvironment")>]
    member this.toResolutionEnvironment() =
         { resolutionFolder = this.ResolutionFolder;
           outputFile = Option.ofObj(this.OutputFile);
           showResolutionMessages = this.ShowResolutionMessages;
           referencedAssemblies = this.ReferencedAssemblies;
           temporaryFolder = this.TemporaryFolder }                        
