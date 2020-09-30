match a with
| :? Foo as bar when
    bar.M && bar.L ->
  ()
