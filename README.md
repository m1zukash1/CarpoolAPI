# CarpoolAPI

CarpoolAPI is a backend API built with ASP.NET Core for a university carpooling application. The API allows students to announce themselves as either drivers or passengers, helping them connect for rides to the university.

## Prerequisites
- Install **.NET 8 SDK**: [Download .NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Setup Instructions

1. Clone the repository:
   ```bash
   git clone https://github.com/m1zukash1/CarpoolAPI.git
   ```
2. Navigate to the project directory:
   ```bash
   cd CarpoolAPI
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Create an ``appsettings.Secrets.json`` file in the project root with the following content, replacing with your own values:

    ```json
      {
        "ConnectionStrings": {
          "DefaultConnection": "Data Source=BackEndDB.db"
        },
        "Jwt": {
          "Key": "this_is_a_very_secure_key_123456",
          "Issuer": "yourdomain.com",
          "Audience": "yourdomain.com"
        }
      }
    ```
5. Run the API:
   ```bash
   dotnet run
   ```
6. Access the API locally:
   ```
   http://localhost:5069/
   ```
