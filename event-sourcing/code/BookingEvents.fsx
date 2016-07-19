// Booking is initialized with this data
type BookingCreationData = 
    {
        LeadPassengerSurname : string
        Email : string
        BookingCost : decimal
    }

type SupplierRef = SupplierRef of string
type Money = GBP of decimal

// The different type of events that can occur on a booking
type BookingEvent = 
    | Created of BookingCreationData
    | PaymentRecieved of Money
    | Confirmation of SupplierRef
    | ReceiptSent
    | LeadPassengerModified of string

let GetEventName (x:BookingEvent) = 
    match FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<BookingEvent>) with
    | case, _ -> case.Name  

// Events that have occurred on Booking 123
let myBookingEvents = 
    [
        Created({ Email = "client@domain.com"; LeadPassengerSurname = "Tailor"; BookingCost = 100M})
        PaymentRecieved(GBP 25M)
        Confirmation(SupplierRef "ABC")
        ReceiptSent
        LeadPassengerModified("Taylor")
    ]

module BookingEventsDb = 

    let private database = 
        [ 123 , myBookingEvents ] |> Map.ofSeq

    let public getBookingEvents id =
        match database.TryFind id with
        | Some events -> events
        | None -> []



