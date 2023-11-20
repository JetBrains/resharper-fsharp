module Test1 =
    type Author = {
        Name: string
        YearBorn: int
    }
    type Book = {
        Title: string
        Year: int
        Author: Author
    }

    let f (book: Book) = { book with Author ={caret} { book.Author with Name = "Author1Updated" } }
    let g (book: Book) = { book with Book.Author = {  book.Author with Name = "Author1Updated" } }

    let h (book: Book) =
        let Author = 5
        { book with Author = { book.Author with Name = "Author1Updated" } }
    let k (book: Book) =
        let Book = 5
        // TODO: should be Book.Author.Name
        { book with Author = { book.Author with Name = "Author1Updated" } }


module Test2 = 
    module A =
        type Author = {
            Name: string
            YearBorn: int
        }
        type Book = {
            Title: string
            Year: int
            Author: Author
        }

    type Book = Author | A
    let f (book: A.Book) = { book with Author = { book.Author with Name = "Author1Updated" } }

    open A
    let g (book: Book) = { book with Author = { book.Author with Name = "Author1Updated" } }


module Test3 = 
    module A =
        type Author = {
            Name: string
            YearBorn: int
        }
        type Book = {
            Title: string
            Year: int
            Author: Author
        }

    open A
    type Book = Author | A

    let f (book: A.Book) = { book with Author = { book.Author with Name = "Author1Updated" } }

module Test4 = 
    type Author = {
        Name: string
        YearBorn: int
    }
    [<RequireQualifiedAccess>]
    type Book = {
        Title: string
        Year: int
        Author: Author
    }

    let f (book: Book) = { book with Author = { book.Author with Name = "Author1Updated" } }

module Test5 = 
    type Author = {
        Name: string
        YearBorn: int
    }
    type Kek = {
        Author: Author
        Foo: int
    }
    type Book = {
        Title: string
        Year: int
        Kek: Kek
    }

    let f (book: Book) = { book with Kek = { book.Kek with Author = { book.Kek.Author with Name = "Author1Updated" } } }
