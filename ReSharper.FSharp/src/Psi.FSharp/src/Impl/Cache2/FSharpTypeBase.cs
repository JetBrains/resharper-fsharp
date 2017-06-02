using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpTypeBase : Class
  {
    public FSharpTypeBase([NotNull] IClassPart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers
    {
      get
      {
        var sigPart = GetPartFromSignature();
        if (sigPart != null)
          return Normalize(sigPart.Modifiers);

        if (myParts == null)
          return MemberDecoration.DefaultValue;

        var decoration = myParts.Modifiers;
        var isHiddenBySignature = (myParts?.GetRoot() as FSharpProjectFilePart)?.HasPairFile ?? false;
        if (isHiddenBySignature)
          decoration.AccessRights = AccessRights.INTERNAL;

        return Normalize(decoration);
      }
    }

    private static MemberDecoration Normalize(MemberDecoration decoration)
    {
      if (decoration.AccessRights == AccessRights.NONE)
        decoration.AccessRights = AccessRights.PUBLIC;

      if (decoration.IsStatic)
      {
        decoration.IsAbstract = true;
        decoration.IsSealed = true;
      }

      decoration.IsStatic = true;
      return decoration;
    }

    [CanBeNull]
    private TypePart GetPartFromSignature()
    {
      for (var part = myParts; part != null; part = part.NextPart)
      {
        var filePart = part.GetRoot() as FSharpProjectFilePart;
        if (filePart?.IsSignaturePart ?? false)
          return part;
      }
      return null;
    }
  }
}