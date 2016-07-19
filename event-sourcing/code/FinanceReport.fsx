
#r "lib/BookingEvents.dll"
open BookingEvents  

(* Report requires outstanding amounts on each booking*)

type FinanceBooking = { OutstandingBalanceInGBP : decimal }

(*
        +----------------+                          +------------------+
        |                |                          |                  |
        |  Booking       | +--------------------->  |  Booking         |
        |                |                          |  After Event     |
        |                |    Apply Event           |                  |
        +----------------+                          +------------------+


        +-----------------+   +---------------+     +-------------------+
        |                 |   |               |     |                   |
        |  Booking        |   |   Event       |     |  Booking          |
        |                 |   |               | +-> |  After Event      |
        |                 |   |               |     |                   |
        +-----------------+   +---------------+     +-------------------+


*)

type FinanceBookingEventHandler = FinanceBooking option -> BookingEvent -> FinanceBooking option

let financeBookingEventHandler financeBooking event  = 
    match financeBooking with
    | None -> 
        (*  No booking provided - this must be the first event
            The outstanding balance is the cost of the booking.
        *)
        let (Created { BookingCost = bookingCost}) = event
        Some { OutstandingBalanceInGBP = bookingCost }
    | Some existingBooking -> 
        match event with
        | PaymentRecieved (GBP amountPaid) -> 
            (*  We only care about the PaymentRecieved event
                Deduct the amount specified in this event from the current outstanding balance
            *)
            let newOutstandingBalance = existingBooking.OutstandingBalanceInGBP - amountPaid
            Some { existingBooking with OutstandingBalanceInGBP = newOutstandingBalance }
        | _ -> 
            (*  This event doesn't impact the outstanding balance    
            *)
            financeBooking


(*

            +-----+                                  +-----+
            | E1  |                            B_0   | E1  |   B_1
            +-----+                                  +-----+

            +-----+                                  +-----+
            | E2  |                            B_1   | E2  |   B_2
            +-----+                                  +-----+

            +-----+                                  +-----+
            | E3  |                            B_2   | E3  |   B_3
            +-----+                                  +-----+

            +-----+                                  +-----+
            | E4  |                            B_3   | E4  |   B_4
            +-----+                                  +-----+

            


*)

let financeBookingModel id = 
    id
    |> BookingEventsDb.getBookingEvents
    |> List.fold financeBookingEventHandler None

printfn "The outstanding balance for this booking: %A" (financeBookingModel 123)