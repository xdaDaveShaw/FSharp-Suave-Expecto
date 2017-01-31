module MyService.Tests

open Expecto
open Service.Model
open Service.Program
open Suave
open Suave.Testing

let emptyGuid = System.Guid.Empty
let validId = "f252e576-d134-4a66-97d2-a84dc13cfeb2"
let sampleDoc = { id = System.Guid(validId); body = "Text" }
let sampleDocJson = sampleDoc |> toJson

let getResponse (wp: Suave.Http.WebPart) =
  let ctx = Suave.Http.HttpContext.empty
  let afterCtx = wp ctx |> Async.RunSynchronously
  afterCtx.Value.response

let getResponseCode wp =
  let response = getResponse wp
  response.status.code

let getResponseContent wp =
  let repsonse = getResponse wp
  match repsonse.content with
  | Suave.Http.HttpContent.Bytes b -> Some (System.Text.Encoding.UTF8.GetString(b))
  | _ -> None

let readResult wp = 
  let code = getResponseCode wp
  let content = getResponseContent wp
  (code, content.Value)

[<Tests>]
let getDocTests = 
  testList "GET tests" [
    testCase "get a document that doesn't exist returns 404" <| fun _ ->
      let getDocStub id = None

      let result = getDocFrom getDocStub validId
      
      let response = readResult result

      Expect.equal response (404, "Document not found") "Expected 404"
  
    testCase "passing invalid guid returns bad request" <| fun _ ->
      let getDocStub id = failwith "Should not have been called"

      let result = getDocFrom getDocStub "{not-a-guid}"
      
      let response = readResult result

      Expect.equal response (400, "id is not a guid") "Expected 400"

    testCase "get a document that exists returns 200" <| fun _ ->
      let getDocStub id = Some sampleDoc

      let result = getDocFrom getDocStub validId
      
      let response = readResult result

      Expect.equal response (200, sampleDocJson) "Should be 200 with JSON"
  ]

[<Tests>]
let storeDocTests = 
  testList "POST tests" [
    testCase "post a valid payload" <| fun _ ->
      let doc : ref<Document option> = ref None
      let storeDocStub d = 
        doc := Some d

      let result = storeDocTo storeDocStub sampleDocJson

      let response = readResult result

      Expect.equal (!doc).Value sampleDoc "Document should have been stored"
      Expect.equal response (200, "") "Response should be 200 with empty content"

    testCase "post an invalid payload" <| fun _ ->
      let storeDocStub d = failwith "Should not have been called"

      let result = storeDocTo storeDocStub "not json"

      let response = readResult result

      Expect.equal response (400, "Invalid payload") "Response should be 400"

    testCase "post with an empty guid is rejected" <| fun _ ->
      let storeDocStub d = failwith "Should not have been called"

      let payload = { id = emptyGuid; body = "Text" } |> toJson

      let result = storeDocTo storeDocStub payload

      let response = readResult result

      Expect.equal response (400, "Invalid payload") "Response should be 400"
  ]

let runWithDefaultConfig = runWith defaultConfig

(*
  Tests against the API using an instance of Suave running
  Intergration tests for the routing aspect.
  Must run sequentially to avoid trying to open the same port twice.
*)
[<Tests>]
let apiTests =
  testSequenced <| testList "API routing tests" [
    testCase "GET from API" <| fun _ ->
      let getDocStub id = Some sampleDoc
      let storeDocStub d = ()
      let app = makeApp getDocStub storeDocStub
      
      let path = sprintf "/api/document/%s" validId
      
      let res = 
        (runWithDefaultConfig app)
        |> req HttpMethod.GET path None

      Expect.equal res sampleDocJson "get should have worked"

    testCase "POST to API" <| fun _ ->
      let getDocStub id = Some sampleDoc
      let storeDocStub d = ()
      let app = makeApp getDocStub storeDocStub
      
      let path = "/api/document"
      
      use data = new System.Net.Http.StringContent(sampleDocJson)

      let res = 
        (runWithDefaultConfig app)
        |> req HttpMethod.POST path (Some data)

      Expect.equal res "" "post should have worked"
  ]