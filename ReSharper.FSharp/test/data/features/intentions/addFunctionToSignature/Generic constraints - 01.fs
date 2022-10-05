module Test

let memoizeBy{caret} (g: 'a -> 'c) (f: 'a -> 'b) =
    let cache =
        System.Collections.Concurrent.ConcurrentDictionary<_, _>(HashIdentity.Structural)

    fun x -> cache.GetOrAdd(Some(g x), lazy (f x)).Force()
