-- ============================================================================
-- LogSystem.Core Database Setup Script
-- ============================================================================
-- Purpose: Creates the core database schema for the LogSystem.Core application
--
-- This script creates two main tables:
-- 1. LogCollection: Stores configuration for different log collections
-- 2. LogCollectionAttribute: Stores dynamic attribute definitions for each log collection
--
-- Note: Dynamic log tables (e.g., Logs_SystemA) are NOT created by this script.
-- They are created programmatically by the application when a new log collection is registered.
--
-- This script is idempotent and can be safely executed multiple times.
-- ============================================================================

-- Set execution options for consistency
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Schema: logcollection
-- ============================================================================
-- Purpose: Creates the "logcollection" schema for storing dynamic log tables
--
-- This schema will contain all dynamically created log tables (e.g., Logs_SystemA).
-- The schema is separate from the "dbo" schema to provide better organization
-- and isolation of log data tables.
--
-- This script is idempotent and can be safely executed multiple times.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'logcollection')
BEGIN
    EXEC('CREATE SCHEMA [logcollection]');
    PRINT 'Schema [logcollection] created successfully.';
END
ELSE
BEGIN
    PRINT 'Schema [logcollection] already exists.';
END
GO

-- ============================================================================
-- Table: LogCollection
-- ============================================================================
-- Purpose: Stores metadata and configuration for each log collection
--
-- Columns:
--   ID                       - Unique identifier for the log collection (auto-incrementing)
--   Name                     - Human-readable name of the log collection
--   ClientId                 - Unique client identifier (business key)
--   TableName                - Name of the dynamic log table (e.g., "Logs_SystemA")
--   LogDurationDays          - Number of days to retain logs before expiration
--   LifecyclePolicyCreated   - Flag indicating if Azure lifecycle policy has been created
--
-- Relationships:
--   Referenced by LogCollectionAttribute (one-to-many)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LogCollection' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[LogCollection]
    (
        [ID]                        BIGINT IDENTITY(1,1) NOT NULL,
        [Name]                      NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [ClientId]                  VARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [TableName]                 NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [LogDurationDays]           INT NOT NULL,
        [LifecyclePolicyCreated]    BIT NOT NULL DEFAULT 0,

        CONSTRAINT [PK_LogCollection] PRIMARY KEY CLUSTERED ([ID] ASC)
    );

    PRINT 'Table [LogCollection] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [LogCollection] already exists.';
END
GO

-- Create unique constraint on ClientId to prevent duplicate client identifiers
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_LogCollection_ClientId' AND object_id = OBJECT_ID('dbo.LogCollection'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_LogCollection_ClientId]
        ON [dbo].[LogCollection] ([ClientId] ASC);

    PRINT 'Unique constraint [UX_LogCollection_ClientId] created successfully.';
END
ELSE
BEGIN
    PRINT 'Unique constraint [UX_LogCollection_ClientId] already exists.';
END
GO

-- Create unique constraint on TableName to prevent duplicate table names
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_LogCollection_TableName' AND object_id = OBJECT_ID('dbo.LogCollection'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_LogCollection_TableName]
        ON [dbo].[LogCollection] ([TableName] ASC);

    PRINT 'Unique constraint [UX_LogCollection_TableName] created successfully.';
END
ELSE
BEGIN
    PRINT 'Unique constraint [UX_LogCollection_TableName] already exists.';
END
GO

-- Create index on Name for efficient lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogCollection_Name' AND object_id = OBJECT_ID('dbo.LogCollection'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LogCollection_Name]
        ON [dbo].[LogCollection] ([Name] ASC);

    PRINT 'Index [IX_LogCollection_Name] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_LogCollection_Name] already exists.';
END
GO

-- ============================================================================
-- Table: LogCollectionAttribute
-- ============================================================================
-- Purpose: Stores dynamic attribute definitions for log extraction and storage
--
-- Columns:
--   ID                   - Unique identifier for the attribute (auto-incrementing)
--   LogCollectionID      - Foreign key reference to the LogCollection table
--   Name                 - Human-readable name of the attribute
--   SqlColumnName        - Actual column name in the dynamic log table
--   AttributeTypeID      - Type of attribute (e.g., "Text", "Integer", "DateTime")
--   ExtractionStyleID    - Extraction method (e.g., "Json" for JSONPath)
--   ExtractionExpression - Expression used to extract the value (e.g., "$.person.name")
--
-- Relationships:
--   References LogCollection (many-to-one)
--
-- Notes:
--   - Each attribute corresponds to a dynamically created column in the log collection's log table
--   - AttributeTypeID determines the SQL data type of the column
--   - ExtractionStyleID + ExtractionExpression define how to extract values from log content
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LogCollectionAttribute' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[LogCollectionAttribute]
    (
        [ID]                    BIGINT IDENTITY(1,1) NOT NULL,
        [LogCollectionID]       BIGINT NOT NULL,
        [Name]                  NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [SqlColumnName]         NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [AttributeTypeID]       VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [ExtractionStyleID]     VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        [ExtractionExpression]  NVARCHAR(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,

        CONSTRAINT [PK_LogCollectionAttribute] PRIMARY KEY CLUSTERED ([ID] ASC)
    );

    PRINT 'Table [LogCollectionAttribute] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [LogCollectionAttribute] already exists.';
END
GO

-- Add foreign key constraint to reference LogCollection table
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LogCollectionAttribute_LogCollection' AND parent_object_id = OBJECT_ID('dbo.LogCollectionAttribute'))
BEGIN
    ALTER TABLE [dbo].[LogCollectionAttribute]
        ADD CONSTRAINT [FK_LogCollectionAttribute_LogCollection]
        FOREIGN KEY ([LogCollectionID])
        REFERENCES [dbo].[LogCollection] ([ID])
        ON DELETE NO ACTION;

    PRINT 'Foreign key [FK_LogCollectionAttribute_LogCollection] created successfully.';
END
ELSE
BEGIN
    PRINT 'Foreign key [FK_LogCollectionAttribute_LogCollection] already exists.';
END
GO

-- Create index on LogCollectionID for efficient lookups by log collection
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogCollectionAttribute_LogCollectionID' AND object_id = OBJECT_ID('dbo.LogCollectionAttribute'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LogCollectionAttribute_LogCollectionID]
        ON [dbo].[LogCollectionAttribute] ([LogCollectionID] ASC);

    PRINT 'Index [IX_LogCollectionAttribute_LogCollectionID] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_LogCollectionAttribute_LogCollectionID] already exists.';
END
GO

-- Create unique index on LogCollectionID + SqlColumnName to prevent duplicate columns per log collection
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_LogCollectionAttribute_LogCollectionID_SqlColumnName' AND object_id = OBJECT_ID('dbo.LogCollectionAttribute'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_LogCollectionAttribute_LogCollectionID_SqlColumnName]
        ON [dbo].[LogCollectionAttribute] ([LogCollectionID] ASC, [SqlColumnName] ASC);

    PRINT 'Index [UX_LogCollectionAttribute_LogCollectionID_SqlColumnName] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [UX_LogCollectionAttribute_LogCollectionID_SqlColumnName] already exists.';
END
GO