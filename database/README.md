# FlexFit Catalog Database Scripts

This folder contains SQL scripts for initializing and migrating the **FlexFitCatalogDb** database.

## Database Name
* Target Database: `FlexFitCatalogDb`
* Monolith Database (Source): `FlexFitDb`

---

## ⚠️ Important Warning
> [!WARNING]
> Always perform a full database backup before running any schema changes or data migration scripts.

---

## Execution Order
1. **Schema Script**: Create the database and all tables/relations.
   * File: [FlexFitCatalogDb.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.sql)
2. **Data Migration Script** (Optional): Copy old data from monolith `FlexFitDb` to `FlexFitCatalogDb`.
   * File: [FlexFitCatalogDb.DataMigration.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.DataMigration.sql)

---

## How to Run Scripts

### Method 1: Using SQL Server Management Studio (SSMS) or Azure Data Studio
1. Connect to your SQL Server instance.
2. Open and execute the [FlexFitCatalogDb.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.sql) script to create the database and set up tables.
3. Open and execute the [FlexFitCatalogDb.DataMigration.sql](file:///d:/SU26_Ki_8/PRN3/PRN232-FLEXFIT/database/FlexFitCatalogDb.DataMigration.sql) script if you want to import data from the original monolith database. *(Note: Ensure the source database `FlexFitDb` exists on the same server instance. Update the database name in the script if it differs.)*

### Method 2: Using EF Core CLI Migrations
If you prefer running EF migrations directly in development:
1. Open terminal inside the `FlexFit.CatalogService` project folder.
2. Run the database update command:
   ```bash
   dotnet ef database update
   ```

---

## How to Regenerate SQL Schema Script
If you modify the models/configuration and add new migrations:
1. Open terminal inside the `FlexFit.CatalogService` project folder.
2. Generate the idempotent script:
   ```bash
   dotnet ef migrations script -i -o ../database/FlexFitCatalogDb.sql
   ```
