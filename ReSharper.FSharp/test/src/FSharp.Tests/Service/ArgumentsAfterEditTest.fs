namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open System.IO
open System.Linq
open JetBrains.DocumentManagers
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open JetBrains.Util
open NUnit.Framework

[<FSharpTest>]
type ArgumentsAfterEditTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "features/service/argumentsAfterEdit"

    member x.ReplaceText(projectFile: IProjectFile, oldText: string, newText: string) =
        let document = projectFile.GetDocument()
        let offset = document.GetText().IndexOf(oldText, StringComparison.Ordinal)
        Assert.That(offset, Is.Not.Negative, $"{projectFile.Name} does not contain: {oldText}")

        do
            use cookie = WriteLockCookie.Create()
            document.ReplaceText(TextRange(offset, offset + oldText.Length), newText)

        projectFile.GetSolution().GetPsiServices().Files.CommitAllDocuments()
        document

    member x.DumpArguments(writer: TextWriter, argsOwners: IFSharpArgumentsOwner[]) =
        let getText (args: seq<IArgument>) =
            args |> Seq.map (fun arg -> if isNull arg then "_" else arg.GetText()) |> String.concat "; "

        for argsOwner in argsOwners do
            use compilationContextCookie = CompilationContextCookie.GetOrCreate(argsOwner.GetResolveContext())
            let cachedArgs = getText argsOwner.ParameterArguments
            let computedArgs = getText (argsOwner.CalculateParameterArguments())
            writer.WriteLine($"{argsOwner.GetText()}: cached [{cachedArgs}], computed [{computedArgs}]")

    override x.DoTest(_: Lifetime, project: IProject) =
        let projectFiles = project.GetAllProjectFiles().ToDictionary(fun projectFile -> projectFile.Name)
        let usagesFile = projectFiles["Usages.fs"].ToSourceFiles().Single().GetPrimaryPsiFile()
        let argsOwners = usagesFile.Descendants<IFSharpArgumentsOwner>().ToEnumerable() |> Array.ofSeq

        x.ExecuteWithGold(fun writer ->
            x.DumpArguments(writer, argsOwners)

            let declarationsFile = projectFiles["Declarations.fs"]
            let unsavedDocument = x.ReplaceText(declarationsFile, "M(a: int, b: int)", "M(b: int, a: int)")
            x.ReplaceText(declarationsFile, "C(a: int, b: int)", "C(b: int, a: int)") |> ignore
            x.ReplaceText(declarationsFile, "A(a: int, b: int)", "A(a: int)") |> ignore
            x.ReplaceText(declarationsFile, "let f (a: int) (b: int)", "let f (a: int)") |> ignore

            writer.WriteLine()
            writer.WriteLine("After the edit of Declarations.fs:")
            x.DumpArguments(writer, argsOwners)
            GC.KeepAlive(unsavedDocument)) |> ignore

    [<Test>]
    member x.``Edit in another file``() =
        x.DoTestSolution("Declarations.fs", "Usages.fs")
