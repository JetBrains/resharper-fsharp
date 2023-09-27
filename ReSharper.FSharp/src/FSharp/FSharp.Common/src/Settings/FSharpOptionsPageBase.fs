namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open System
open System.Linq.Expressions
open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.IDE.UI.Extensions
open JetBrains.IDE.UI.Options
open JetBrains.UI.RichText

[<AbstractClass>]
type FSharpOptionsPageBase(lifetime, optionsPageContext, settings) =
    inherit BeSimpleOptionsPage(lifetime, optionsPageContext, settings)

    member x.AddString(text: string, property: IProperty<string>) =
        let grid = [| property.GetBeTextBox(lifetime).WithDescription(text, lifetime) |].GetGrid()
        x.AddControl(grid)
        x.AddKeyword(text)

    member x.AddString(text: string, getter: Expression<Func<_,_>>) =
        x.AddString(text, settings.GetValueProperty(lifetime, getter))

    member x.AddDescription(text: string) =
        use indent = x.Indent()
        x.AddRichText(RichText(text)) |> ignore

    member x.AddBool(text: string, property: IProperty<_>) =
        x.AddBoolOption(property, RichText(text), text) |> ignore

    member x.AddHeader(text: string) =
        base.AddHeader(text) |> ignore
