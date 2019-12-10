namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System
open System.Collections.Generic
open System.Text
open System.Threading
open JetBrains.Lifetimes
open JetBrains.Metadata.Utils
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Host.Features.ProjectModel
open JetBrains.ReSharper.Host.Features.ProjectModel.View
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.FsiDetector
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model
open JetBrains.Util

[<SolutionComponent>]
type FsiHost
        (lifetime: Lifetime, solution: ISolution, fsiDetector: FsiDetector, fsiOptions: FsiOptionsProvider,
         projectModelViewHost: ProjectModelViewHost, psiModules: IPsiModules) =

    let stringArg = sprintf "--%s:%O"
    let boolArg option arg = sprintf "--%s%s" option (if arg then "+" else "-")

    let stringArrayArgs (arg: string) =
        arg.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)

    let getNewFsiSessionInfo _ =
        let fsi =
            if fsiOptions.AutoDetect.Value then
                fsiDetector.GetAutodetected(solution)
            else
                fsiDetector.GetActiveTool(solution, fsiOptions)

        let fsiPath = fsi.GetFsiPath(fsiOptions.UseAnyCpu.Value)

        let args =
            [| yield! stringArrayArgs fsiOptions.FsiArgs.Value
               yield! stringArrayArgs fsiOptions.FsiInternalArgs.Value

               yield boolArg "shadowcopyreferences" fsiOptions.ShadowCopyReferences.Value

               if fsiOptions.SpecifyLanguageVersion.Value then
                   yield FSharpLanguageVersion.toCompilerArg fsiOptions.LanguageVersion.Value

               if PlatformUtil.IsRunningUnderWindows then
                   yield stringArg "fsi-server-output-codepage" Encoding.UTF8.CodePage
                   yield stringArg "fsi-server-input-codepage"  Encoding.UTF8.CodePage

               yield stringArg "fsi-server-lcid" Thread.CurrentThread.CurrentUICulture.LCID |]

        RdFsiSessionInfo(fsiPath.FullPath, fsi.Runtime, fsi.IsCustom, List(args), fsiOptions.FixOptionsForDebug.Value)

    let assemblyFilter (assembly: IPsiAssembly) =
        let assemblyName = assembly.AssemblyName
        not (isFSharpCore assemblyName || assemblyName.PossiblyContainsPredefinedTypes())

    let getProjectReferences projectId =
        use cookie = ReadLockCookie.Create()

        let project = projectModelViewHost.GetItemOrThrow<IProject>(projectId)
        let targetFrameworkId = project.GetCurrentTargetFrameworkId()
        let psiModule = psiModules.GetPrimaryPsiModule(project, targetFrameworkId)

        getReferencePaths assemblyFilter psiModule

    do
        let rdFsiHost = solution.RdFSharpModel.FSharpInteractiveHost
        rdFsiHost.RequestNewFsiSessionInfo.Set(getNewFsiSessionInfo)
        rdFsiHost.GetProjectReferences.Set(getProjectReferences)

        fsiOptions.MoveCaretOnSendLine.FlowInto(lifetime, rdFsiHost.MoveCaretOnSendLine)
        fsiOptions.ExecuteRecent.FlowInto(lifetime, rdFsiHost.CopyRecentToEditor)
