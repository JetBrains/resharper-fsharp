if true then
      1
      |> id
else 1
     |> id
|> id

if true then
      1
      |> id
elif true then
      1
      |> id
else 1
      |> id
|> id



match true with
| true ->
      1
      |> id
| false ->
      1
      |> id

match true with
| true ->
      1
      |> id
| false ->
      1
      |> id
|> id



1
|> fun x -> x
          |> id

1
|> (fun x -> x
             |> id)

1
|> fun x -> (x
             |> id)
|> id



[1]
|> List.map (fun x ->
      x
      |> id)

let f x y z = 0
[1]
|> f (fun x ->
        x
        |> id) 1
