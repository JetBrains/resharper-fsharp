namespace global

type Author = {
    Name: string
    YearBorn: int
}
type Book = {
    Title: string
    Year: int
    Author: Author
}

module Book =
    type Author = {
        Name: string
        YearBorn: int
    }
    type Book = {
        Title: string
        Year: int
        Author: Author
    }


namespace Namespace1

type Author = {
    Name: string
    YearBorn: int
}
type Book = {
    Title: string
    Year: int
    Author: Author
}


namespace Test1
    module Test1 =
        let f (book: Book) = { book with Author ={caret} { book.Author with Name = "Author1Updated" } }
        let g (book: Book.Book) = { book with Author = { book.Author with Name = "Author1Updated" } }
        let h (book: Namespace1.Book) = { book with Author = { book.Author with Name = "Author1Updated" } }


namespace Test2
    module Test2 =
        open Namespace1
        let f (book: Book) = { book with Author = { book.Author with Name = "Author1Updated" } }

        open Book
        let g (book: Book) = { book with Author = { book.Author with Name = "Author1Updated" } }
        let h (book: Namespace1.Book) = { book with Author = { book.Author with Name = "Author1Updated" } }
