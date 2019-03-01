namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp

type FSharpProjectLanguage(name, presentableName, projectFileType) =
    inherit ProjectLanguage(name, presentableName, projectFileType)

    static member val Instance =
        FSharpProjectLanguage("FSharp", "F#", fun () -> FSharpProjectFileType.Instance :> _) :> ProjectLanguage
