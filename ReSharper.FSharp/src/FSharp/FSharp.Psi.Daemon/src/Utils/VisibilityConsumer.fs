module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Utils.VisibleRangeContainer

open System.Collections.Generic
open JetBrains.DocumentModel

type VisibilityConsumer<'a>(visibleRange: DocumentRange, getRange: 'a -> DocumentRange) =
    let visibleRangeIsValid = visibleRange.IsValid()
    let visible = List()
    let notVisible = List()
    let getContainer item =
        if visibleRangeIsValid && (getRange item).IntersectsOrContacts(&visibleRange) then visible
        else notVisible

    member x.Add(item) =
        let listToAdd = getContainer item
        listToAdd.Add(item)

    //TODO: optimize
    member x.AddRange(items) =
        if items |> Seq.forall (fun x -> getContainer x = notVisible) then notVisible.AddRange(items)
        else for item in items do x.Add(item)

    member x.VisibleItems = visible
    member x.NotVisibleItems = notVisible
