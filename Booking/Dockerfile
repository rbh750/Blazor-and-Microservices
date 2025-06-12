# Use the official .NET 9 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies.
COPY Booking/Booking.csproj Booking/
COPY AzServices/AzServices.csproj AzServices/
RUN dotnet restore Booking/Booking.csproj
RUN dotnet restore AzServices/AzServices.csproj

# Copy the rest of the source code
COPY Booking/. Booking/
COPY AzServices/. AzServices/

# Build and publish the Booking API project
RUN dotnet publish Booking/Booking.csproj -c Release -o /app/publish

# Use the official ASP.NET runtime image for the final container not the entire SDK.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Booking.dll"]