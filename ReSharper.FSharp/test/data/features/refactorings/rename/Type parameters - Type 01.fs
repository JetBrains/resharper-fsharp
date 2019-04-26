module Module

type R<'a> =
    { Field: 'a }

    member x.Prop: 'a{caret} = Unchecked.defaultof<_>
