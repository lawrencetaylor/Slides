#if INTERACTIVE
#load "FinanceReport.fsx"
#load "DispatchReport.fsx"
#endif

#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "./packages/Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"

open BookingEvents

module EventStore = 

    open FSharp.Data
    open Newtonsoft.Json
    open System

    let private serialize = JsonConvert.SerializeObject
    let private deserializeBookingEvent json : BookingEvent = JsonConvert.DeserializeObject<BookingEvent>(json)

    type PostEventRequest = 
        {
            EventId : string
            EventType : string
            Data : obj
        }
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module private PostEventRequest = 
        let create (eventType, eventData) = 
            {
                EventId = Guid.NewGuid().ToString()
                EventType = eventType
                Data = eventData
            } |> Array.singleton

    let private addEventAsync host port streamName body = 
        Http.AsyncRequest(
            sprintf "http://%s:%i/streams/%s" host port streamName,
            body = TextRequest body,
            httpMethod = HttpMethod.Post,
            headers = [ HttpRequestHeaders.ContentType "application/vnd.eventstore.events+json"] )

    type private Events = JsonProvider<"sample.json">

    let private extractEvents response = 
        response
        |> Events.Parse
        |> (fun eventEnvelope -> eventEnvelope.Entries)
        |> Seq.map(fun entry -> entry.Data)
        |> Seq.map(deserializeBookingEvent)

    let  getEvents host port stream = 
        Http.AsyncRequest(
            sprintf "http://%s:%i/streams/%s?embed=body" host port stream,
            httpMethod = HttpMethod.Get,
            headers = [ HttpRequestHeaders.Accept "application/vnd.eventstore.events+json"])
        |> Async.RunSynchronously
        |> (fun r ->
            let (Text body) =  r.Body
            body |> extractEvents |> Seq.rev)

    let publish host port streamName  = 
        PostEventRequest.create
        >> serialize
        >> addEventAsync host port streamName


module BookingAdministrator = 

    open BookingEvents

    let saveEvent host port streamName event =
        (event |> GetEventName, event :> obj) 
        |> EventStore.publish host port streamName
        |> Async.RunSynchronously
        |> ignore

let toEventStoreStream id = sprintf "Booking-%i" id 

let saveAllMyBookingEvents id = 
    id 
    |> BookingEventsDb.getBookingEvents 
    |> Seq.iter(BookingAdministrator.saveEvent "boot2docker" 2113 (id |> toEventStoreStream))

let financeModel id  = 
    id
    |> toEventStoreStream
    |> EventStore.getEvents "boot2docker" 2113
    |> Seq.fold(FinanceReport.financeBookingEventHandler) None

let dispatchModel id = 
    id
    |> toEventStoreStream
    |> EventStore.getEvents "boot2docker" 2113
    |> Seq.fold(DispatchReport.dispatchBookingEventHandler) None



123 |> saveAllMyBookingEvents |> ignore

123 |> financeModel |> printfn "Finance model is: %A" |> ignore
123 |> dispatchModel |> printfn "Dispatch model is: %A" |> ignore
        
