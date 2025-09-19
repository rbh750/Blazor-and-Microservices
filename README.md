# Booking Microservices Solution

This repository contains a microservices-based solution for simulating movie seat bookings. It consists of two main components:

## 1. Blazor WebApp
- **Technology:** Blazor Server (.NET 9)
- **Purpose:** Provides an interactive UI for users to simulate movie seat bookings.
- **Features:**
  - Displays a grid of seats for selected movies.
  - Receives real-time seat status updates (Available, Held, Reserved) via Azure Service Bus messages.
  - Visually paints reserved seats and updates seat status dynamically.
  - Demonstrates resilience: If the Booking API triggers a circuit breaker, the UI continues to function independently, showing the last known seat states.

## 2. Booking Web API
- **Technology:** ASP.NET Core Web API (.NET 9)
- **Purpose:** Simulates the booking process and communicates seat status changes.
- **Features:**
  - Simulates seat bookings for movies, randomly holding and reserving seats.
  - Publishes seat status updates to Azure Service Bus topics.
  - Implements a circuit breaker pattern: If too many seats are reserved, the API temporarily stops processing, simulating a failure scenario.

## How It Works
- The Blazor WebApp requests a booking simulation from the API.
- The API simulates bookings and sends seat status updates to Azure Service Bus.
- The WebApp listens for these messages and updates the UI accordingly.
- If the API circuit breaker is triggered, the WebApp continues to display the last received seat statuses, demonstrating decoupling and resilience.

## Getting Started
1. **Prerequisites:**
   - .NET 9 SDK
   - Docker (optional, for containerization)
   - Azure Service Bus connection string
2. **Running Locally:**
   - Start the Booking API (see `Booking/Booking.csproj`).
   - Start the Blazor WebApp (see `WebApp/WebApp.csproj`).
   - Configure connection strings in `appsettings.json`.
3. **Containerization:**
   - See `Booking/Dockerfile` for API containerization instructions.

## Circuit Breaker Demo
The Booking API will trigger a circuit breaker if more than half of the seats are reserved. During this period, the API will pause processing, but the Blazor WebApp will continue to function, showing the last known seat states.

## Solution Structure
- `WebApp/` - Blazor UI project
- `Booking/` - ASP.NET Core Web API project
- `AzServices/` - Shared services and entities

## License
This project is for demonstration and educational purposes.
