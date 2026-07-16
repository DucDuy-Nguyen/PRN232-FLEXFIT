# FlexFit Catalog Service Handoff Document

This document provides necessary technical details for team members to integrate and consume the **FlexFit.CatalogService** microservice.

---

## 1. Business Scope & Ownership
The Catalog Service owns the Gym, Branch, Category, Class, GymSession, and BranchStaff mapping domains:
* Gym & GymImage CRUD.
* Branch, BranchImage & GymAmenity CRUD.
* Class, ClassSchedule & GymSession configurations.
* Staff assignment mapping (`BranchId` <-> `StaffId`).
* Favorite Gym and Favorite Class listings.

*Note: Booking capacity configuration belongs to Catalog, but seat reservation/actual counts belong to Booking Service.*

---

## 2. Technical Stack & Architecture
* **Language/Runtime**: C# on .NET 8.0
* **Framework**: ASP.NET Core Web API & gRPC Server
* **Database**: SQL Server (`FlexFitCatalogDb`)
* **DbContext**: `CatalogDbContext`
* **Message Broker**: Redis Streams (`catalog-stream`)

---

## 3. Endpoints & API Specifications
* **REST Base URL**: `http://localhost:5002`
* **gRPC Address**: `http://localhost:5003`
* **Health Check**: `http://localhost:5002/health`
* **Swagger Documentation**: `http://localhost:5002/swagger`

---

## 4. gRPC Contract & Files
* **Proto Location**: [/FlexFit.CatalogService/Protos/catalog.proto](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/FlexFit.CatalogService/Protos/catalog.proto)
* **Offered RPCs**:
  1. `GetClassBookingSnapshot`: Takes `class_id` Guid string and returns a complete scheduling and capacity snapshot for booking validation.
  2. `GetGymSessionBookingSnapshot`: Takes `session_id` Guid string and returns a complete snapshot of the gym session.

---

## 5. Redis Streams Events
* **Stream Name**: `catalog-stream`
* **Published Events**:
  1. `StaffAssignedToBranchEvent`
     * Payload: `{ "StaffId": "guid", "BranchId": "guid" }`
  2. `StaffRemovedFromBranchEvent`
     * Payload: `{ "StaffId": "guid", "BranchId": "guid" }`

---

## 6. Database Files
* **Schema Initialization SQL**: [FlexFitCatalogDb.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.sql)
* **Data Migration SQL**: [FlexFitCatalogDb.DataMigration.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.DataMigration.sql)
* **Readme**: [database/README.md](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/README.md)

---

## 7. Required Environment Variables
Configure the following in the host environment or Docker Compose:
* `CATALOG_DB_CONNECTION_STRING`: Connection string to SQL Server database `FlexFitCatalogDb`.
* `JWT_KEY`: Symmetric key to validate signature (must match monolith/identity secret).
* `JWT_ISSUER`: Expected token issuer (e.g. `FlexFit`).
* `JWT_AUDIENCE`: Expected token audience (e.g. `FlexFitClients`).
* `REDIS_CONNECTION_STRING`: Redis host address (e.g. `localhost:6379`).

---

## 8. Gateway Routing Configuration
Integrate routes from [docs/catalog-gateway-routes.md](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/docs/catalog-gateway-routes.md) into the API Gateway configuration.

---

## 9. Docker Compose Snippet
To run the Catalog Service in Docker Compose along with SQL Server and Redis:

```yaml
version: '3.8'

services:
  catalog.db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong@Password
    ports:
      - "1433:1433"

  catalog.redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  catalog.service:
    build:
      context: ./FlexFit.CatalogService
      dockerfile: Dockerfile
    ports:
      - "5002:5002"
      - "5003:5003"
    environment:
      - CATALOG_DB_CONNECTION_STRING=Server=catalog.db;Database=FlexFitCatalogDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True
      - JWT_KEY=FlexFitSuperSecretKeyOfAtLeast32BytesLength!
      - JWT_ISSUER=FlexFit
      - JWT_AUDIENCE=FlexFitClients
      - REDIS_CONNECTION_STRING=catalog.redis:6379
    depends_on:
      - catalog.db
      - catalog.redis
```
