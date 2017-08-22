using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  public class FSharpLookupItem : TextLookupItemBase
  {
    [NotNull] private readonly string myText;
    private readonly bool myIsEscaped;

    public FSharpLookupItem([NotNull] string text, [NotNull] IconId image, bool isEscaped) : base(false)
    {
      myText = text;
      myIsEscaped = isEscaped;
      Image = image;
    }

    public override IconId Image { get; }

    public override string Text => myIsEscaped ? "``" + myText + "``" : myText;

    protected override RichText GetDisplayName()
    {
      return LookupUtil.FormatLookupString(myText, TextColor);
    }
  }
}