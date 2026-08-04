-- Migration Script: Add ChiTietDatTruoc table and update Reservation
-- Generated: 2026-07-20

-- Step 1: Backup existing data from Reservation
SELECT * INTO #ReservationBackup FROM Reservation;

-- Step 2: Create ChiTietDatTruoc table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietDatTruoc]') AND type in (N'U'))
BEGIN
	CREATE TABLE [ChiTietDatTruoc] (
		[MaChiTiet] INT IDENTITY(1,1) PRIMARY KEY,
		[MaReservation] INT NOT NULL,
		[MaSach] VARCHAR(10) NULL,
		[SoLuong] INT NOT NULL DEFAULT 1,
		[GhiChu] NVARCHAR(255) NULL,
		CONSTRAINT [FK_ChiTietDatTruoc_Reservation_MaReservation] FOREIGN KEY ([MaReservation]) 
			REFERENCES [Reservation]([MaReservation]) ON DELETE CASCADE
	);

	-- Create indices
	CREATE INDEX [IX_ChiTietDatTruoc_MaReservation] ON [ChiTietDatTruoc] ([MaReservation]);
	CREATE INDEX [IX_ChiTietDatTruoc_MaSach] ON [ChiTietDatTruoc] ([MaSach]);

	PRINT 'Created ChiTietDatTruoc table';
END;

-- Step 3: Migrate data from old Reservation.MaSach to ChiTietDatTruoc
-- If the data exists, copy it
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
		   WHERE TABLE_NAME='Reservation' AND COLUMN_NAME='MaSach')
BEGIN
	INSERT INTO ChiTietDatTruoc (MaReservation, MaSach, SoLuong)
	SELECT MaReservation, MaSach, 1 
	FROM #ReservationBackup 
	WHERE MaSach IS NOT NULL;

	PRINT 'Migrated existing reservations to ChiTietDatTruoc';
END;

-- Step 4: Add MaNV and GhiChu columns to Reservation if they don't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
			   WHERE TABLE_NAME='Reservation' AND COLUMN_NAME='MaNV')
BEGIN
	ALTER TABLE [Reservation] ADD [MaNV] NVARCHAR(10) NULL;
	PRINT 'Added MaNV column to Reservation';
END;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
			   WHERE TABLE_NAME='Reservation' AND COLUMN_NAME='GhiChu')
BEGIN
	ALTER TABLE [Reservation] ADD [GhiChu] NVARCHAR(255) NULL;
	PRINT 'Added GhiChu column to Reservation';
END;

-- Step 5: Skip FK for MaNV - will be handled by EF Core if needed
-- The data types should already match, but we let EF Core manage relationships

PRINT 'Skipping manual FK creation for MaNV (will be handled by EF Core)';

-- Step 6: Drop MaSach column from Reservation if it exists
-- First disable all constraints to prevent cascading issues
EXEC sp_MSForEachTable 'ALTER TABLE ? DISABLE TRIGGER ALL';

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
		   WHERE TABLE_NAME='Reservation' AND COLUMN_NAME='MaSach')
BEGIN
	-- Find and drop all foreign keys on MaSach
	DECLARE @sql NVARCHAR(MAX);
	SELECT @sql = ISNULL(@sql + '; ', '') + 'ALTER TABLE [' + TABLE_NAME + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']'
	FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE
	WHERE COLUMN_NAME = 'MaSach' AND TABLE_NAME = 'Reservation';

	IF @sql IS NOT NULL
		EXEC sp_executesql @sql;

	-- Then drop the column
	ALTER TABLE [Reservation] DROP COLUMN [MaSach];
	PRINT 'Dropped MaSach column from Reservation';
END;
ELSE
BEGIN
	PRINT 'MaSach column does not exist or already dropped';
END;

-- Re-enable all constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? ENABLE TRIGGER ALL';

-- Step 7: Clean up backup
DROP TABLE #ReservationBackup;

PRINT 'Migration completed successfully!';
