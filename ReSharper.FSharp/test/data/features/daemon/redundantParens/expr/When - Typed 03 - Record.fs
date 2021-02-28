module Module

match () with
| _ when { F = () :? unit } = null -> ()
