namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Application.Settings
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.EditorConfig
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.EditorConfig
open NUnit.Framework

[<FSharpTest>]
type FantomasEditorConfigTest() =
    inherit ConfigReadTestBase()

    override x.ConfigName = "editorConfig"

    override x.RelativeTestDataPath = "features/fantomas"

    [<Test>] member x.``Fantomas settings``() = x.DoNamedTestFolder()

    override this.ProjectItems testProject =
        testProject.GetSubItemsRecursively() |> Seq.filter _.Name.EndsWith(".fs")

    override this.ProcessSingleTestData(_, testData, sourceFile, defaultContext, optimization, writer) =
        let ecContext = testData.BoundStore.SettingsStore.BindToContextTransientWithEditorConfig(sourceFile);
        let noEcContext = testData.BoundStore.SettingsStore.BindToContextTransient(SettingLayerTypeDataContextUtils.ClassicApplicationWideContextRange);

        this.ProcessIndexedEntry((fun (x: FSharpFormatSettingsKey) -> x.FantomasSettings), defaultContext, noEcContext, ecContext, optimization, writer, "fsharp")
