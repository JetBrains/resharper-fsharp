(* language=json *)
let s1 = """
         [ {
              "key": null
           } ]
         """

let s2 = (*language=xml*) """
                          <tag1>
                            <tag2 attr="value"/>
                          </tag1>
                          """

// language=sql
let s3 = """INSERT INTO Person (42, "Joe", "Doe")"""

(* language=js *)
let s4 = """alert("Hello")