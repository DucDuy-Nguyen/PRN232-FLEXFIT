# API Gateway Route Configuration - Catalog Service

This document defines the routing rules that the API Gateway should apply to forward requests to the **FlexFit.CatalogService** microservice.

## Service Details
* **Service Name**: `FlexFit.CatalogService`
* **Internal Base URL**: `http://localhost:5002` (REST API)
* **Internal gRPC Port**: `5003` (gRPC Endpoint)
* **Health Check**: `http://localhost:5002/health`

---

## Route Mappings

| Public Route Pattern | Target Internal Route | HTTP Methods | Required Role | JWT Forwarding |
| :--- | :--- | :--- | :--- | :--- |
| `/api/gyms` | `http://localhost:5002/api/gyms` | `GET` | Anonymous / Any | Optional |
| `/api/gyms` | `http://localhost:5002/api/gyms` | `POST` | `Admin` | Required |
| `/api/gyms/{id}` | `http://localhost:5002/api/gyms/{id}` | `GET` | Anonymous / Any | Optional |
| `/api/gyms/{id}` | `http://localhost:5002/api/gyms/{id}` | `PUT`, `DELETE` | `GymPartner`, `Admin` | Required |
| `/api/gyms/{id}/status` | `http://localhost:5002/api/gyms/{id}/status` | `PATCH` | `Admin`, `GymPartner` | Required |
| `/api/gyms/partner` | `http://localhost:5002/api/gyms/partner` | `GET` | `GymPartner` | Required |
| `/api/gyms/transfer-owner` | `http://localhost:5002/api/gyms/transfer-owner` | `PUT` | `GymPartner` | Required |
| `/api/branches` | `http://localhost:5002/api/branches` | `GET` | Anonymous / Any | Optional |
| `/api/branches` | `http://localhost:5002/api/branches` | `POST` | `GymPartner` | Required |
| `/api/branches/{id}` | `http://localhost:5002/api/branches/{id}` | `GET` | Anonymous / Any | Optional |
| `/api/branches/{id}` | `http://localhost:5002/api/branches/{id}` | `PUT`, `DELETE` | `GymPartner` | Required |
| `/api/branches/{id}/status` | `http://localhost:5002/api/branches/{id}/status` | `PATCH` | `GymPartner` | Required |
| `/api/branches/{id}/amenities` | `http://localhost:5002/api/branches/{id}/amenities` | `PUT` | `GymPartner`, `Staff` | Required |
| `/api/branches/{id}/images` | `http://localhost:5002/api/branches/{id}/images` | `PUT` | `GymPartner`, `Staff` | Required |
| `/api/branches/partner` | `http://localhost:5002/api/branches/partner` | `GET` | `GymPartner` | Required |
| `/api/branches/assign-staff` | `http://localhost:5002/api/branches/assign-staff` | `POST` | `GymPartner` | Required |
| `/api/branches/assign-staff-by-email`| `http://localhost:5002/api/branches/assign-staff-by-email` | `POST` | `GymPartner` | Required |
| `/api/branches/remove-staff` | `http://localhost:5002/api/branches/remove-staff` | `DELETE` | `GymPartner` | Required |
| `/api/branches/update-staff` | `http://localhost:5002/api/branches/update-staff` | `PUT` | `GymPartner` | Required |
| `/api/amenities` | `http://localhost:5002/api/amenities` | `GET` | Anonymous / Any | Optional |
| `/api/amenities` | `http://localhost:5002/api/amenities` | `POST` | `Admin` | Required |
| `/api/amenities/{id}` | `http://localhost:5002/api/amenities/{id}` | `PUT`, `DELETE` | `Admin` | Required |
| `/api/categories` | `http://localhost:5002/api/categories` | `GET` | Anonymous / Any | Optional |
| `/api/categories` | `http://localhost:5002/api/categories` | `POST` | `Admin` | Required |
| `/api/categories/{id}` | `http://localhost:5002/api/categories/{id}` | `GET` | Anonymous / Any | Optional |
| `/api/categories/{id}` | `http://localhost:5002/api/categories/{id}` | `PUT`, `DELETE` | `Admin` | Required |
| `/api/classes` | `http://localhost:5002/api/classes` | `GET` | Anonymous / Any | Optional |
| `/api/classes` | `http://localhost:5002/api/classes` | `POST` | `GymPartner` | Required |
| `/api/classes/{id}` | `http://localhost:5002/api/classes/{id}` | `GET` | Anonymous / Any | Optional |
| `/api/classes/{id}` | `http://localhost:5002/api/classes/{id}` | `PUT`, `DELETE` | `GymPartner` | Required |
| `/api/classes/{id}/status` | `http://localhost:5002/api/classes/{id}/status` | `PATCH` | `GymPartner` | Required |
| `/api/classes/branch/{branchId}` | `http://localhost:5002/api/classes/branch/{branchId}` | `GET` | Anonymous / Any | Optional |
| `/api/classes/staff-schedule` | `http://localhost:5002/api/classes/staff-schedule` | `GET` | `Staff` | Required |
| `/api/classes/partner` | `http://localhost:5002/api/classes/partner` | `GET` | `GymPartner` | Required |
| `/api/favorite-gyms/toggle/{id}` | `http://localhost:5002/api/favorite-gyms/toggle/{id}` | `POST` | `Member` | Required |
| `/api/favorite-gyms/my-list` | `http://localhost:5002/api/favorite-gyms/my-list` | `GET` | `Member` | Required |
| `/api/favorite-classes/toggle/{id}` | `http://localhost:5002/api/favorite-classes/toggle/{id}` | `POST` | `Member` | Required |
| `/api/favorite-classes/my-list` | `http://localhost:5002/api/favorite-classes/my-list` | `GET` | `Member` | Required |

---

## Gateway Requirements
1. **JWT Header Forwarding**: The gateway MUST validate and forward the incoming Bearer JWT token in the `Authorization` header to Catalog Service endpoints.
2. **Path Preserving**: The gateway should preserve the URL paths and forward them directly, stripping only the gateway prefix if applicable.
3. **CORS Configuration**: The gateway should handle preflight requests (`OPTIONS`) and headers appropriate for frontend integrations.
