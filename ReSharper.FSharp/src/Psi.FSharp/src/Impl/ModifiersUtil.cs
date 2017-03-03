using System.Collections.Generic;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class ModifiersUtil
  {
    public static MemberDecoration GetDecoration(IAccessModifiers accessModifiers)
    {
      var decoration = MemberDecoration.DefaultValue;
      var modifiers = new JetHashSet<string>();

      // todo: rewrite using token types
      foreach (var modifier in accessModifiers.Modifiers)
        modifiers.Add(modifier.GetText());

      if (modifiers.Contains("public")) decoration.Modifiers |= Modifiers.PUBLIC;
      if (modifiers.Contains("protected")) decoration.Modifiers |= Modifiers.PROTECTED;
      if (modifiers.Contains("internal")) decoration.Modifiers |= Modifiers.INTERNAL;
      if (modifiers.Contains("private")) decoration.Modifiers |= Modifiers.PRIVATE;

      return decoration;
    }
  }
}