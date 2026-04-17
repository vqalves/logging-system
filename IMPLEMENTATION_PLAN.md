# LogSystem.Core Implementation Plan

## Project Overview

LogSystem.Core is a .NET 8.0 class library designed to provide log storage and retrieval capabilities using a hybrid approach:
- **Azure Blob Storage**: For storing compressed log file content with TTL-based expiration
- **SQL Server Database**: For storing log metadata and dynamic attributes with flexible schema evolution

The system supports dynamic attribute extraction from log content (currently JSON via JSONPath) and provides flexible querying with multiple filter operators.

---

## Architecture Summary

### Core Components
- **DatabaseService**: Manages SQL Server operations for systems, attributes, and log persistence/querying
- **AzureService**: Handles Azure Blob Storage operations for log file upload/download with gzip compression
- **LogExtractionService**: Extracts structured log data from raw content using configurable extraction styles
- **Models**: System, LogAttribute, Log, LogFilter, AttributeType, ExtractionStyle hierarchy

### Database Schema
- **Core Tables**: `System`, `SystemLogAttribute` (defined in SQL script)
- **Dynamic Tables**: One table per system (e.g., `Logs_SystemA`) with fixed + dynamic columns, created programmatically

---

## Implementation Steps

### Phase 1: Database Foundation (DBA)

#### Step 1.1: Create Database Setup Script
**File**: `/home/vinicius/Documents/Projects/log-system/database-setup.sql`

**Responsibilities**:
- Create `System` table with columns: `ID` (BIGINT IDENTITY PK), `Name` (NVARCHAR), `TableName` (NVARCHAR), `LogDurationHours` (BIGINT)
- Create `SystemLogAttribute` table with columns: `ID` (BIGINT IDENTITY PK), `SystemID` (BIGINT FK), `Name` (NVARCHAR), `SqlColumnName` (NVARCHAR), `AttributeTypeID` (VARCHAR), `ExtractionStyleID` (VARCHAR), `ExtractionExpression` (NVARCHAR)
- Add appropriate primary keys, foreign keys, and indexes
- Use case-insensitive collation `SQL_Latin1_General_CP1_CI_AS` for text columns where applicable
- Include comments documenting table purposes and relationships
- No dynamic log tables in this script (created by application)

**Validation**: Script should be idempotent and executable on a fresh SQL Server instance

---

### Phase 2: Configuration Setup (Backend Developer)

#### Step 2.1: Implement DatabaseConfig
**File**: `Services/Database/DatabaseConfig.cs`

**Responsibilities**:
- Add property for SQL Server connection string (sourced from environment variables)
- Ensure connection string supports integrated security or SQL authentication as needed

#### Step 2.2: Implement AzureConfig
**File**: `Services/Azure/AzureConfig.cs`

**Responsibilities**:
- Add property for Azure Blob Storage connection string (sourced from environment variables)
- Add property for container name if needed (or hardcode if always the same)

**Validation**: Configuration classes should be simple POCOs ready for dependency injection

---

### Phase 3: Azure Blob Storage Integration (Backend Developer)

#### Step 3.1: Add NuGet Package
**Action**: Add `Azure.Storage.Blobs` package to `LogSystem.Core.csproj`

#### Step 3.2: Implement AzureService.UploadFileAsync
**File**: `Services/Azure/AzureService.cs:7-13`

**Responsibilities**:
- Initialize `BlobServiceClient` using `AzureConfig` connection string
- Get reference to blob at path `/logs/v1/{systemName}/{fileName}`
- Compress content using `GZipStream` to byte array or stream
- Upload compressed content to blob
- Set blob metadata with TTL expiration based on `fileDuration` parameter
- Handle blob already exists scenario (overwrite or throw exception as appropriate)

**Technical Notes**:
- Use `BlobUploadOptions` to set metadata
- Expiration should be managed via blob lifecycle management policy or custom metadata

#### Step 3.3: Implement AzureService.DownloadFileAsync
**File**: `Services/Azure/AzureService.cs:15-20`

**Responsibilities**:
- Initialize `BlobServiceClient` using `AzureConfig` connection string
- Get reference to blob at path `/logs/v1/{systemName}/{fileName}`
- Check if blob exists; return `DownloadedFile` with `Found=false` if not
- Download blob content stream
- Decompress using `GZipStream`
- Return `DownloadedFile` with `Found=true` and `Content` populated

**Error Handling**: Handle blob not found gracefully, propagate other Azure exceptions

**Validation**: Test upload/download round-trip with sample gzipped content

---

### Phase 4: JSON Extraction Implementation (Backend Developer)

#### Step 4.1: Add NuGet Package
**Action**: Verify `System.Text.Json` is available (part of .NET 8.0 SDK)

#### Step 4.2: Implement ExtractionStyleJson.Extract
**File**: `Services/Database/Models/ExtractionStyles/ExtractionStyleJson.cs:14-17`

**Responsibilities**:
- Accept `content` parameter as `string` or `JsonDocument` (choose based on LogExtractionService integration)
- Parse JSONPath expression (e.g., `$.person.name`)
- Navigate JSON structure using `System.Text.Json.JsonDocument` or `JsonNode`
- Return extracted value as appropriate type (string, int, DateTime)
- Handle missing paths gracefully (return null or throw meaningful exception)

**Technical Notes**:
- Use `JsonDocument.Parse` for read-only parsing
- Implement custom JSONPath parser or use lightweight library if needed
- Optimize to avoid repeated parsing in LogExtractionService

**Validation**: Test with sample JSON and various JSONPath expressions

---

### Phase 5: Log Extraction Service (Backend Developer)

#### Step 5.1: Implement LogExtractionService.Extract
**File**: `Services/Database/LogExtractionService.cs:5-12`

**Responsibilities**:
- Create new `Log` instance with `SourceFileIndex` and `SourceFileName`
- Calculate `ValidUntilUtc` based on current UTC time + `system.LogDurationHours`
- Parse `logContent` once (if JSON, use `JsonDocument.Parse`)
- Iterate through `attributes` collection
- For each attribute, determine extraction style from `attribute.ExtractionStyleID`
- Delegate to appropriate `ExtractionStyle.Extract` method (e.g., `ExtractionStyleJson.Extract`)
- Populate `Log.Attributes` dictionary with `SqlColumnName` as key and extracted value as value
- Handle type conversion based on `AttributeType` (text, integer, datetime)
- Optimize to avoid unnecessary allocations and parsing

**Technical Notes**:
- Reuse parsed `JsonDocument` across all attributes for same log
- Dispose `JsonDocument` properly
- Handle extraction failures gracefully (log warning, set attribute to null, or throw)

**Validation**: Test with sample log content and multiple attributes of different types

---

### Phase 6: Database Service - System Management (Backend Developer + DBA)

#### Step 6.1: Implement DatabaseService.SaveSystemAsync
**File**: `Services/Database/DatabaseService.cs:30-36`

**Responsibilities**:
- Check if `system.ID == 0` (insert) or `system.ID != 0` (update)
- **For INSERT**:
  - Execute `INSERT INTO System` with `Name`, `TableName`, `LogDurationHours`
  - Retrieve `SCOPE_IDENTITY()` and update `system.ID`
  - Create dynamic log table using `system.TableName` with fixed columns:
    - `ID` (BIGINT IDENTITY PRIMARY KEY)
    - `SourceFileIndex` (INT NOT NULL)
    - `SourceFileName` (NVARCHAR(500) NOT NULL)
    - `ValidUntilUtc` (DATETIME2 NOT NULL)
  - Add index on `ValidUntilUtc` for cleanup queries
- **For UPDATE**:
  - Execute `UPDATE System` with `Name`, `LogDurationHours` (TableName should be immutable)
- Use `Microsoft.Data.SqlClient` with parameterized queries

**SQL Injection Prevention**: Always use SqlParameter for all values

**Validation**: Test insert creates table correctly, update preserves table

---

### Phase 7: Database Service - Attribute Management (Backend Developer + DBA)

#### Step 7.1: Implement DatabaseService.CreateAttributeAsync
**File**: `Services/Database/DatabaseService.cs:12-20`

**Responsibilities**:
- Insert record into `SystemLogAttribute` table with all LogAttribute properties
- Retrieve inserted `ID` via `SCOPE_IDENTITY()` and update `logAttribute.ID`
- Execute `ALTER TABLE {system.TableName} ADD {logAttribute.SqlColumnName} {sqlDataType} NULL`
  - Get `sqlDataType` from `AttributeType.Parse(logAttribute.AttributeTypeID).SqlDataType`
  - For VARCHAR columns, use collation `SQL_Latin1_General_CP1_CI_AS`
- Create filtered index: `CREATE INDEX IX_{system.TableName}_{logAttribute.SqlColumnName} ON {system.TableName}({logAttribute.SqlColumnName}) WHERE {logAttribute.SqlColumnName} IS NOT NULL`
  - For VARCHAR columns, index should be case-insensitive (inherit from column collation)

**Safety**: Validate `SqlColumnName` to prevent SQL injection (alphanumeric + underscore only)

**Transaction**: Wrap insert + ALTER TABLE in transaction for atomicity

**Validation**: Test creates column and index correctly for all attribute types

#### Step 7.2: Implement DatabaseService.DeleteAttributeAsync
**File**: `Services/Database/DatabaseService.cs:22-28`

**Responsibilities**:
- Delete record from `SystemLogAttribute` table where `ID = logAttribute.ID`
- Check if column exists in `system.TableName` using `sys.columns` catalog view
- If column exists:
  - Drop associated indexes using `sys.indexes` and `sys.index_columns` catalog views
  - Execute `ALTER TABLE {system.TableName} DROP COLUMN {logAttribute.SqlColumnName}`
- Handle case where column doesn't exist gracefully (no error)

**Safety**: Validate `SqlColumnName` to prevent SQL injection

**Transaction**: Wrap delete + DROP operations in transaction

**Validation**: Test deletes attribute and cleans up column/indexes

---

### Phase 8: Database Service - Log Persistence (Backend Developer + DBA)

#### Step 8.1: Implement DatabaseService.SaveLogsAsync
**File**: `Services/Database/DatabaseService.cs:38-46`

**Responsibilities**:
- Count logs in `IEnumerable<Log>`
- **If count < 10**: Use batch INSERT statements
  - Build parameterized INSERT for each log
  - Include fixed columns: `SourceFileIndex`, `SourceFileName`, `ValidUntilUtc`
  - Include dynamic columns from `Log.Attributes` dictionary
  - Use `DBNull.Value` for null attribute values
  - Execute in single transaction
- **If count >= 10**: Use `SqlBulkCopy`
  - Create `DataTable` with schema matching `system.TableName`
  - Add fixed columns and all dynamic columns from first log's attributes
  - Populate rows from logs collection
  - Use `DBNull.Value` for null attribute values
  - Execute `SqlBulkCopy.WriteToServerAsync` with transaction

**Performance**: Use async methods throughout, minimize allocations

**Error Handling**: Rollback transaction on failure

**Validation**: Test both batch INSERT and SqlBulkCopy paths with various log counts

---

### Phase 9: Database Service - Log Querying (Backend Developer + DBA)

#### Step 9.1: Implement DatabaseService.QueryLogsAsync
**File**: `Services/Database/DatabaseService.cs:48-55`

**Responsibilities**:
- Build dynamic SQL query: `SELECT {columns} FROM {system.TableName} WHERE {filters}`
- **Column Selection**:
  - Include fixed columns: `ID`, `SourceFileIndex`, `SourceFileName`, `ValidUntilUtc`
  - Include dynamic columns from `attributes` parameter (map to `LogAttribute.SqlColumnName`)
- **Filter Construction**:
  - Iterate through `filters` collection
  - Map `LogFilter.LogAttributeID` to `LogAttribute.SqlColumnName` from `attributes` collection
  - Build WHERE clause based on `FilterOperator`:
    - `Equal`: `{column} = @param`
    - `NotEqual`: `{column} != @param`
    - `LessThan`: `{column} < @param`
    - `LessThanOrEqual`: `{column} <= @param`
    - `GreaterThan`: `{column} > @param`
    - `GreaterThanOrEqual`: `{column} >= @param`
    - `Contains`: `{column} LIKE '%' + @param + '%'`
    - `StartsWith`: `{column} LIKE @param + '%'`
    - `EndsWith`: `{column} LIKE '%' + @param`
    - `IsNull`: `{column} IS NULL`
    - `IsNotNull`: `{column} IS NOT NULL`
  - Combine filters with `AND` operator
  - Use parameterized queries with `SqlParameter` for all values
- **Type Conversion**: Cast `LogFilter.Value` to appropriate type based on `AttributeType`
- **Execution**:
  - Execute query using `SqlDataReader`
  - Yield return each row as `Log` instance with populated `Attributes` dictionary
  - Use `IAsyncEnumerable` for streaming results

**SQL Injection Prevention**: Validate all column names against whitelist, use parameters for values

**Performance**: Use `CommandBehavior.SequentialAccess` for large result sets

**Validation**: Test with various filter combinations and attribute types

---

### Phase 10: Testing & Validation (Backend Developer + DBA)

#### Step 10.1: Database Script Testing (DBA)
- Execute `database-setup.sql` on clean SQL Server instance
- Verify tables created with correct schema
- Verify foreign keys and indexes exist
- Test re-running script (idempotency)

#### Step 10.2: Integration Testing (Backend Developer)
- Test full workflow:
  1. Create new System via `SaveSystemAsync`
  2. Add LogAttributes via `CreateAttributeAsync`
  3. Extract logs from JSON via `LogExtractionService.Extract`
  4. Save logs via `SaveLogsAsync` (test both batch paths)
  5. Query logs via `QueryLogsAsync` with various filters
  6. Upload log file via `AzureService.UploadFileAsync`
  7. Download log file via `AzureService.DownloadFileAsync`
  8. Delete attribute via `DeleteAttributeAsync`

#### Step 10.3: Edge Case Testing (Backend Developer)
- Test with null attribute values
- Test with empty log collections
- Test with non-existent blobs
- Test with invalid JSONPath expressions
- Test with SQL column name validation
- Test concurrent attribute creation/deletion

---

## Dependencies & Prerequisites

### DBA Prerequisites
- SQL Server instance (2016 or later recommended)
- Permissions to create databases, tables, indexes
- Understanding of dynamic SQL and schema evolution patterns

### Backend Developer Prerequisites
- .NET 8.0 SDK installed
- Azure Storage Account with Blob Service enabled
- Connection strings for both SQL Server and Azure Blob Storage
- NuGet packages: `Azure.Storage.Blobs`, `Microsoft.Data.SqlClient`

---

## Execution Order

1. **DBA**: Step 1.1 (Database setup script)
2. **Backend**: Step 2.1, 2.2 (Configuration classes)
3. **Backend**: Step 3.1, 3.2, 3.3 (Azure integration)
4. **Backend**: Step 4.1, 4.2 (JSON extraction)
5. **Backend**: Step 5.1 (Log extraction service)
6. **Backend + DBA**: Step 6.1 (System management with table creation)
7. **Backend + DBA**: Step 7.1, 7.2 (Attribute management with DDL)
8. **Backend + DBA**: Step 8.1 (Log persistence with bulk operations)
9. **Backend + DBA**: Step 9.1 (Dynamic query building)
10. **DBA + Backend**: Step 10.1, 10.2, 10.3 (Testing)

---

## Key Technical Decisions

| Decision | Rationale |
|----------|-----------|
| SqlBulkCopy threshold at 10 logs | Balance between setup overhead and bulk insert performance |
| GZip compression for blob storage | Standard compression with good .NET support |
| Case-insensitive collation | Matches typical log search patterns |
| JSONPath for extraction | Industry standard for JSON querying |
| Filtered indexes on nullable columns | Optimize queries while minimizing index size |
| IAsyncEnumerable for queries | Enable streaming of large result sets |
| Dynamic SQL with validation | Flexibility for schema evolution with security |

---

## Security Considerations

- **SQL Injection**: All dynamic SQL must validate identifiers against whitelist (alphanumeric + underscore), use SqlParameter for values
- **Connection Strings**: Store in environment variables, never hardcode
- **Blob Access**: Use connection strings with appropriate permissions (read/write only, not admin)
- **Collation**: Case-insensitive for usability, but be aware of security implications for sensitive data

---

## Performance Optimizations

- **JSON Parsing**: Parse once per log in LogExtractionService, reuse across attributes
- **Bulk Operations**: Use SqlBulkCopy for >10 logs
- **Indexed Columns**: Filtered indexes with NOT NULL predicate reduce index size
- **Streaming Queries**: IAsyncEnumerable prevents loading entire result set into memory
- **Blob Compression**: GZip reduces storage costs and transfer time

---

## Future Extensibility

The architecture supports future enhancements:
- Additional ExtractionStyle implementations (Regex, CSV, XML)
- Additional AttributeType implementations (Decimal, Boolean, JSON)
- Additional FilterOperator implementations (In, Between)
- Multi-tenant isolation via schema or database separation
- Read replicas for query scaling

---

## Deliverables

### DBA Deliverables
1. `database-setup.sql` - Idempotent script creating core tables
2. Documentation of schema design decisions
3. Validation report of script execution

### Backend Developer Deliverables
1. Completed implementation of all TODO items across 7 files
2. Updated `LogSystem.Core.csproj` with required NuGet packages
3. Integration test demonstrating full workflow
4. Documentation of configuration requirements (environment variables)

---

## Estimated Effort

| Phase | Estimated Time | Assignee |
|-------|---------------|----------|
| Phase 1: Database Foundation | 2-4 hours | DBA |
| Phase 2: Configuration Setup | 1 hour | Backend |
| Phase 3: Azure Integration | 3-4 hours | Backend |
| Phase 4: JSON Extraction | 2-3 hours | Backend |
| Phase 5: Log Extraction Service | 2-3 hours | Backend |
| Phase 6: System Management | 3-4 hours | Backend + DBA |
| Phase 7: Attribute Management | 4-5 hours | Backend + DBA |
| Phase 8: Log Persistence | 4-5 hours | Backend + DBA |
| Phase 9: Log Querying | 5-6 hours | Backend + DBA |
| Phase 10: Testing | 4-6 hours | Backend + DBA |
| **Total** | **30-41 hours** | |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| SQL injection via dynamic SQL | Strict validation of all identifiers, parameterized queries |
| Schema lock during ALTER TABLE | Use short transactions, consider maintenance windows |
| Large log volume performance | Implement batch size limits, consider partitioning for future |
| Azure throttling | Implement retry policies with exponential backoff |
| JSON parsing errors | Graceful error handling, detailed logging |

---

**End of Implementation Plan**
