using JetBrains.Annotations;
using JetBrains.Util;

namespace JetBrains.Platform.ProjectModel.FSharp.Properties
{
  public class FSharpProjectLanguage : EnumPattern
  {
    public FSharpProjectLanguage([NotNull] string name) : base(name)
    {
    }
  }
}