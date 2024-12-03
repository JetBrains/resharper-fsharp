module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Utils.VisibleRangeContainer

open System.Collections.Generic
open JetBrains.DocumentModel
open JetBrains.Util

type VisibilityConsumer<'a>(visibleRange: DocumentRange, getRange: 'a -> DocumentRange) =
    let visible: ICollection<'a> = if visibleRange.IsValid() then List() else EmptyList.Instance
    let notVisible: ICollection<'a> = List()
    let getContainer item =
        let itemRange = getRange item
        if visibleRange.IntersectsOrContacts(&itemRange) then visible else notVisible

    member x.Add(item) =
        let listToAdd = getContainer item
        listToAdd.Add(item)

    member x.AddRange(items) =
        for item in items do x.Add(item)

    member x.HasVisibleItems = visible.Count > 0
    member x.VisibleItems = visible
    member x.NonVisibleItems = notVisible
