# FlexFit Catalog Service

The Catalog Service is an independent microservice responsible for Gyms, Branches, Categories, Classes, and Session management within the FlexFit ecosystem.

---

## 1. Business Boundary & Ownership
The Catalog Service owns and maintains:
* **Gym**: Name, description, thumbnail, rating averages, owner contact.
* **Branch**: Gym locations, details, opening/closing hours, amenity mappings, branch images, and branch staff assignments.
* **Category**: Grouping for fitness classes.
* **Class & GymSession**: Name, difficulty level, capacity limits, credit cost, and schedule.
* **FavoriteGym / FavoriteClass**: User's liked items (treated as a temporary technical debt here to facilitate easy transition, which can be moved to an Engagement Service later).
* **BranchStaff**: Mapping of `BranchId` to `StaffId` (user scalar ID).

---

## 2. Capacity Ownership Principle
* **Catalog Service** owns the **capacity configuration** (maximum seats) for classes/sessions.
* **Booking Service** owns the **seat reservations** and booking transactions.
* **Catalog Service does not count bookings** or track current occupancy; Booking Service handles transaction locking and booking verification against the capacity configured in the Catalog snapshot.

---

## 3. Layered Architecture
This project is built using a layered architecture:
* **API / Controllers**: REST Controllers and gRPC Service endpoints.
* **Service / Application**: Coordinates business logic and processes domain entities.
* **Repository / Infrastructure**: Accesses the Entity Framework Core context (`CatalogDbContext`) and publishes events via StackExchange.Redis.

---

## 4. REST Endpoints Summary
All REST endpoints are documented and testable via Swagger:
* **Gyms**: `/api/gyms/**`
* **Branches**: `/api/branches/**`
* **Amenities**: `/api/amenities/**`
* **Categories**: `/api/categories/**`
* **Classes**: `/api/classes/**`
* **Favorites**: `/api/favorite-gyms/**` & `/api/favorite-classes/**`

### Search, Filter, Sort, Pagination
Implemented on:
* `GET /api/gyms` (search by name, filter by status and ownerId, sort by name or rating)
* `GET /api/classes` (search by name, filter by branchId, categoryId, status, sort by name, credit cost, or start time)

Query parameters:
* `pageNumber` (default: 1)
* `pageSize` (default: 10, max: 100)
* `search`
* `status`
* `sortBy`
* `sortDirection`

---

## 5. JWT Authorization
Access to endpoints is restricted using JWT tokens validated against standard role validation:
* **Admin**: Manage Amenities, Categories, creating Gyms, and changing Gym status.
* **GymPartner**: Create Branches, Classes, Gym Sessions, and manage branch staff.
* **Staff**: Update branch-level details (amenities, images).
* **Member**: Manage Favorite lists.

---

## 6. gRPC Specifications
The service acts as a gRPC Server on port `5003`.
* **Proto File**: [Protos/catalog.proto](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/FlexFit.CatalogService/Protos/catalog.proto)
* **Offered RPCs**:
  * `GetClassBookingSnapshot`: Returns class name, capacity, credit cost, and schedule.
  * `GetGymSessionBookingSnapshot`: Returns session details.

---

## 7. Redis Streams Events
When branch staff assignments change, events are published to Redis Streams.
* **Stream Name**: `catalog-stream`
* **Events**:
  * `StaffAssignedToBranchEvent`
  * `StaffRemovedFromBranchEvent`

---

## 8. Configuration Variables
Defined in `appsettings.json` or as environment variables:
* `CATALOG_DB_CONNECTION_STRING`: Connection string to SQL Server database `FlexFitCatalogDb`.
* `JWT_KEY`: Token signing key.
* `JWT_ISSUER`: Expected issuer.
* `JWT_AUDIENCE`: Expected audience.
* `REDIS_CONNECTION_STRING`: Redis connection URL.

---

## 9. Run the Service Local
Run the service directly using dotnet:
```bash
dotnet restore
dotnet run
```

### Run Migration
To apply EF Core migrations to database:
```bash
dotnet ef database update
```

### Generate SQL Script
To regenerate the database schema script:
```bash
dotnet ef migrations script -i -o ../database/FlexFitCatalogDb.sql
```

### Run Docker
Build the docker image:
```bash
docker build -t flexfit-catalog-service .
```

---

## 10. Verification & Testing

### Swagger REST Testing
Open Swagger UI in your browser:
* URL: `http://localhost:5002/swagger`
* To authenticate, click **Authorize** at the top right, and enter: `Bearer <your_jwt_token>`

### Health Check Testing
Check the service health status:
* URL: `http://localhost:5002/health`
* Output: `Healthy` (returns `Unhealthy` if SQL Server or database is offline)

### Redis Stream Testing
To check events published to the Redis Stream:
1. Connect using Redis CLI: `redis-cli`
2. Read stream entries:
   ```bash
   XREAD COUNT 10 STREAMS catalog-stream 0
   ```
