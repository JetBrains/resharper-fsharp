namespace global

open System

type I =
     abstract M1<'T when 'T :> ICloneable> : value: 'T -> Unit
     abstract M2<'T when 'T : struct> : value: 'T -> Unit
