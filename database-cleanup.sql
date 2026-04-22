-- ============================================================================
-- LogSystem.Core Database Cleanup Script
-- ============================================================================
-- Purpose: Drops all database objects created by database-setup.sql
--
-- WARNING: This script will permanently delete:
-- 1. All dynamically created log tables in the [logcollection] schema
-- 2. LogCollectionAttribute table and all its data
-- 3. LogCollection table and all its data
-- 4. The [logcollection] schema
--
-- This operation is IRREVERSIBLE. All log data will be lost.
-- ============================================================================

-- Set execution options for consistency
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================================';
PRINT 'Starting LogSystem.Core Database Cleanup';
PRINT '============================================================================';
PRINT '';
GO

-- ============================================================================
-- Step 1: Drop all dynamically created log tables in [logcollection] schema
-- ============================================================================

PRINT 'Step 1: Dropping all dynamic log tables in [logcollection] schema...';
PRINT '';

DECLARE @DynamicTableName NVARCHAR(255);
DECLARE @DropDynamicTableSQL NVARCHAR(MAX);

DECLARE dynamic_table_cursor CURSOR FOR
    SELECT TABLE_NAME
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'logcollection'
      AND TABLE_TYPE = 'BASE TABLE';

OPEN dynamic_table_cursor;
FETCH NEXT FROM dynamic_table_cursor INTO @DynamicTableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @DropDynamicTableSQL = 'DROP TABLE [logcollection].[' + @DynamicTableName + '];';
    EXEC sp_executesql @DropDynamicTableSQL;
    PRINT 'Dropped table [logcollection].[' + @DynamicTableName + ']';

    FETCH NEXT FROM dynamic_table_cursor INTO @DynamicTableName;
END

CLOSE dynamic_table_cursor;
DEALLOCATE dynamic_table_cursor;

PRINT '';
PRINT 'All dynamic log tables dropped successfully.';
PRINT '';
GO

-- ============================================================================
-- Step 2: Drop LogCollectionAttribute table
-- ============================================================================

PRINT 'Step 2: Dropping LogCollectionAttribute table...';
PRINT '';

-- Drop foreign key constraint first
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LogCollectionAttribute_LogCollection' AND parent_object_id = OBJECT_ID('dbo.LogCollectionAttribute'))
BEGIN
    ALTER TABLE [dbo].[LogCollectionAttribute]
        DROP CONSTRAINT [FK_LogCollectionAttribute_LogCollection];
    PRINT 'Foreign key [FK_LogCollectionAttribute_LogCollection] dropped.';
END
ELSE
BEGIN
    PRINT 'Foreign key [FK_LogCollectionAttribute_LogCollection] does not exist.';
END
GO

-- Drop the table
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LogCollectionAttribute' AND type = 'U')
BEGIN
    DROP TABLE [dbo].[LogCollectionAttribute];
    PRINT 'Table [LogCollectionAttribute] dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Table [LogCollectionAttribute] does not exist.';
END

PRINT '';
GO

-- ============================================================================
-- Step 3: Drop LogCollection table
-- ============================================================================

PRINT 'Step 3: Dropping LogCollection table...';
PRINT '';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LogCollection' AND type = 'U')
BEGIN
    DROP TABLE [dbo].[LogCollection];
    PRINT 'Table [LogCollection] dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Table [LogCollection] does not exist.';
END

PRINT '';
GO

-- ============================================================================
-- Step 4: Drop logcollection schema
-- ============================================================================

PRINT 'Step 4: Dropping [logcollection] schema...';
PRINT '';

IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'logcollection')
BEGIN
    DROP SCHEMA [logcollection];
    PRINT 'Schema [logcollection] dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Schema [logcollection] does not exist.';
END

PRINT '';
GO

-- ============================================================================
-- Cleanup Complete
-- ============================================================================

PRINT '============================================================================';
PRINT 'LogSystem.Core Database Cleanup Completed Successfully';
PRINT 'All tables, constraints, and schemas have been removed.';
PRINT '============================================================================';
GO
