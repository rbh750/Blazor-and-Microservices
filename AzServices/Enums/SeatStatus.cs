namespace AzServices.Enums;

public enum SeatStatus
{
    Available,   // The seat is free and can be selected
    Held,        // The seat is selected (held) but not yet paid
    Reserved     // The seat is paid and fully reserved
}