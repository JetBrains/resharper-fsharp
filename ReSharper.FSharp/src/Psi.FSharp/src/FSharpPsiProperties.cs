using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Impl;

namespace JetBrains.ReSharper.Psi.FSharp
{
  public class FSharpPsiProperties : DefaultPsiProjectFileProperties
  {
    public FSharpPsiProperties([NotNull] IProjectFile projectFile, [NotNull] IPsiSourceFile sourceFile)
      : base(projectFile, sourceFile)
    {
    }

    public override bool ProvidesCodeModel =>
//      ProjectFile.Properties.BuildAction.IsCompile() ||
//      ProjectFile.LanguageType.Equals(FSharpScriptProjectFileType.Instance);
      true;

    public override bool ShouldBuildPsi => true;
  }
}