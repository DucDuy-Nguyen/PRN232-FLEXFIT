# Local Frontend Integration with Payment Service

This document details the configuration requirements and temporary testing endpoints for integrating the frontend application with the Payment microservice.

## Integration Parameters

1. **Local Frontend URL**:
   `http://localhost:5173`
   
2. **Payment API Base URL**:
   `http://localhost:5211`

3. **Temporary Endpoint**:
   `POST /dev/token` (only available in the `Development` environment).

## Next Steps for Integration

* **Auth Service JWT Configuration**:
  The Payment Service's configuration for validating JWT tokens must exactly match the parameters issued by the Auth Service:
  - `Jwt:Key`
  - `Jwt:Issuer`
  - `Jwt:Audience`

* **Clean Up**:
  Make sure to remove or disable all references to the temporary `/dev/token` endpoint once the main Auth Service is deployed and active.
  
* **Webhook Notice**:
  PayOS webhook callbacks cannot reach `localhost` directly in a production configuration without a tunneling mechanism (e.g., ngrok).
  
* **Production Credentials**:
  Do not commit real PayOS credentials or database connection strings to the version control repository.

---

> [!NOTE]
> **Leader Integration Note**:
> Replace the temporary local development token generation flow (`/dev/token`) with the authentic JWT tokens issued by the Auth Service once the services are integrated.
