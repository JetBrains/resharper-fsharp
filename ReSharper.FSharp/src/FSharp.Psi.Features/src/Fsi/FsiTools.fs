namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open JetBrains.Rider.Model

module FsiTools =
    let wrapCommand (commandIndex: int, command: string) =
        "[<AutoOpen>]\nmodule module" + (commandIndex + 1).ToString() + "=\n  " + command.Replace("\n", "\n  ") + "\n  ignore\n"
        
        
    let prepareCommands (rdFsiPrepareCommandsArgs: RdFsiPrepareCommandsArgs): ResizeArray<string> =
        let firstCommandIndex = rdFsiPrepareCommandsArgs.FirstCommandIndex
        let commands: ResizeArray<string> = rdFsiPrepareCommandsArgs.Commands
        
        commands |> Seq.mapi(fun i x -> wrapCommand(firstCommandIndex + i, x)) |> ResizeArray
