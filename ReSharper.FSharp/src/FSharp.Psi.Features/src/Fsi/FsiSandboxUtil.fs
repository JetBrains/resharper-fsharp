namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System.Collections.Generic
open FSharp.Compiler
open JetBrains.ReSharper.Plugins.FSharp

module FsiSandboxUtil =
    let prepareCommands (rdFsiPrepareCommandsArgs: RdFsiPrepareCommandsArgs): List<string> =
        let firstCommandIndex = rdFsiPrepareCommandsArgs.FirstCommandIndex

        let wrapCommand (commandIndex: int) (commandText: string) =
            let moduleIndex = firstCommandIndex + commandIndex + 1
            let moduleName = sprintf "%s%04d" PrettyNaming.FsiDynamicModulePrefix moduleIndex

            // Add () to make the command not parse as a module abbreviation.
            let text = commandText.Replace("\n", "\n  ") + "\n  ()\n"

            // Add Obsolete to hide modules from lookup.
            sprintf "[<AutoOpen; System.Obsolete>]\nmodule %s =\n  %s" moduleName text

        rdFsiPrepareCommandsArgs.Commands |> Seq.mapi wrapCommand |> List
