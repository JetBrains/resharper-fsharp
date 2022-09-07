module Module

type A =
    abstract member M: 'T -> 'T when 'T :> IDisposable
