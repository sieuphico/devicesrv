-- Optimize Database for DeviceSrv
-- Adding Indexes to speed up searching and sorting

-- 1. Enable pg_trgm extension for fast ILIKE searches (requires superuser or appropriate schema permissions)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 2. Indexes for "Models" table
-- B-Tree for sorting by Name and filtering by Category
CREATE INDEX IF NOT EXISTS idx_models_name ON public."Models" ("Name");
CREATE INDEX IF NOT EXISTS idx_models_category ON public."Models" ("Category");

-- 3. Indexes for "Devices" table
-- B-Tree for sorting and FK
CREATE INDEX IF NOT EXISTS idx_devices_modelid ON public."Devices" ("ModelId");
CREATE INDEX IF NOT EXISTS idx_devices_name_btree ON public."Devices" ("Name");
CREATE INDEX IF NOT EXISTS idx_devices_imei_btree ON public."Devices" ("Imei");
CREATE INDEX IF NOT EXISTS idx_devices_sn_btree ON public."Devices" ("SerialNumber");

-- 4. GIN Indexes for ILIKE %filter% searches using pg_trgm
-- This significantly speeds up ILIKE searches on large text columns
CREATE INDEX IF NOT EXISTS idx_devices_name_trgm ON public."Devices" USING gin ("Name" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_devices_imei_trgm ON public."Devices" USING gin ("Imei" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_devices_sn_trgm ON public."Devices" USING gin ("SerialNumber" gin_trgm_ops);

-- Analysis
ANALYZE public."Models";
ANALYZE public."Devices";
