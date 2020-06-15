let inline callTypeFunction<'T when 'T : (static member Two : 'T)> =
    (^T : (static member Two : 'T) ())

let inline callTypeFunction2<'T> =
	typeof<'T>
