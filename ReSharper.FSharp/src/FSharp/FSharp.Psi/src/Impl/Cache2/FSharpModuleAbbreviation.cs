using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;

public class FSharpModuleAbbreviation([NotNull] Class.IClassPart part) : FSharpClass(part), ILanguageSpecificDeclaredElement
{
    protected override bool AcceptsPart(TypePart part) =>
        part is FSharpModuleAbbreviationPart;

    public bool IsErased => true;
}
