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

///Interface with the AWS Store
module DocStore =

    open System
    open System.Collections.Generic
    open Model

    //Fake Doc Store
    let store = 
        new Dictionary<Guid, String>()

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
            match document with
            | Some d -> d.id <>Guid.Empty
            | _ -> false
        
        let document = fromJson text
        let isValid = validate document
        
        match (document, isValid) with 
        | (Some document as d, true) -> storeDocImpl d.Value |> ignore; OK ""
        | (_,_) -> BAD_REQUEST "Invalid payload"

    let makeApp fGet fStore = 
        choose
            [ 
              GET >=> choose
                [ pathScan "/api/document/%s" (getDocFrom fGet) ]
              POST >=> choose
                [ path "/api/document" >=> request (fun req -> storeDocTo fStore (System.Text.Encoding.UTF8.GetString(req.rawForm))) ]
            ]

    [<EntryPoint>]
    let main argv =
        let app = makeApp DocStore.getDocById DocStore.storeDoc
        startWebServer defaultConfig app
        0