namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open System.Linq
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type GenerateSignatureFileTest() =
    inherit FSharpContextActionExecuteTestBase<GenerateSignatureFileAction>()
    
    override this.ExtraPath = "generateSignatureFile"
    
    // [<Test>] member x.``ModuleStructure - 01`` () = x.DoNamedTestWithSignature()