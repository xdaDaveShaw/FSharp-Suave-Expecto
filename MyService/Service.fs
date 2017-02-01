namespace Service

module Model =
    type Document = 
        { id: System.Guid;
          body: string }

    let fromJson json = 
        try
            Some <| Newtonsoft.Json.JsonConvert.DeserializeObject<Document>(json)
        with
            | _ -> None
    
    let toJson doc = Newtonsoft.Json.JsonConvert.SerializeObject(doc)

///Simple implementation of a document store using a Dictionary.
///Should be Mocked when testing because this is where the impure code would go.
module DocStore =

    open System
    open System.Collections.Generic
    open Model

    let store = Dictionary()

    let getDocById (id:Guid) = 
        match store.TryGetValue(id) with
        | (true, v) -> Some { id = id; body = v; }
        | _ -> None

    let storeDoc document = 
        store.[document.id] <- document.body

///Actual Web Server
module Program =

    open System
    open Suave
    open Suave.Filters
    open Suave.RequestErrors
    open Suave.Successful
    open Suave.Operators
    open Model

    let parseGuid s = 
        match Guid.TryParse s with
        | (true, guid) -> Some guid
        | _ -> None

    let getDocFrom (getDocImpl: Guid -> Document option) (id: string) = 
        match parseGuid id with
        | Some id ->
            match getDocImpl id with
            | Some document -> OK(document |> toJson)
            | None -> NOT_FOUND "Document not found"
        | None -> BAD_REQUEST "id is not a guid"

    let storeDocTo (storeDocImpl: Document -> unit) text =
        let validate document =
            if document.id <> Guid.Empty then Some document
            else None
        
        let document = text |> fromJson |> Option.bind validate
        
        match document with 
        | Some document -> storeDocImpl document |> ignore; OK ""
        | _ -> BAD_REQUEST "Invalid payload"

    let makeApp fGet fStore = 
        choose
            [ GET >=> pathScan "/api/document/%s" (getDocFrom fGet)
              POST >=> path "/api/document" >=> request (fun req -> storeDocTo fStore (System.Text.Encoding.UTF8.GetString(req.rawForm))) ]

    [<EntryPoint>]
    let main argv =
        let app = makeApp DocStore.getDocById DocStore.storeDoc
        startWebServer defaultConfig app
        0