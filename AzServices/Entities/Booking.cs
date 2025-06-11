using AzServices.Enums;

namespace AzServices.Entities;

public record Booking(
    int Row,
    int Number,
    SeatStatus Status,
    string Movie
);