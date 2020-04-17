namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open JetBrains.Rider.Model
open System.Collections.Generic

type FsiTools() =
    static let wrapCommand (commandIndex: int, command: string) =
        let autoOpenString = "[<AutoOpen>]"
        let moduleString = "module module"
        let ignoreString = "ignore"
        command.Split('\n') |> Array.toList |> List.fold(fun acc codeString -> acc + "    " + codeString + "\n") "" |>
            fun newCommand -> autoOpenString + "\n" + moduleString + commandIndex.ToString() + "=\n"
                              + newCommand + "    " + ignoreString + "\n"
        
    static member prepareCommands (rdFsiPrepareCommandsArgs: RdFsiPrepareCommandsArgs) : List<string> =
        let firstCommandIndex = rdFsiPrepareCommandsArgs.FirstCommandIndex
        let commands = rdFsiPrepareCommandsArgs.Commands
        let result = List()
        let mutable i = 1
        for command in commands do
            result.Add(wrapCommand(firstCommandIndex + i, command))
            i <- i + 1
            
        result
