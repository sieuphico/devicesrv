-- Database Initialization for DeviceSrv
-- User: postgres, Pass: root, DB: DeviceSrvDb
-- Updated with double-quoted identifiers for case-sensitivity

CREATE TABLE IF NOT EXISTS public."Models" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Manufacturer" TEXT,
    "Category" TEXT,
    "Subcategory" TEXT,
    "Available" INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS public."Devices" (
    "Id" SERIAL PRIMARY KEY,
    "ModelId" INT NOT NULL REFERENCES public."Models"("Id") ON DELETE CASCADE,
    "Name" TEXT,
    "Imei" TEXT,
    "SerialLab" TEXT,
    "SerialNumber" TEXT,
    "Cicuiseri" TEXT,
    "Hwversion" TEXT,
    "IsBorrowed" BOOLEAN DEFAULT FALSE
);

-- Sample Data
INSERT INTO public."Models" ("Name", "Manufacturer", "Category", "Subcategory", "Available") VALUES 
('iPhone 15', 'Apple', 'Mobile', 'Smartphone', 2),
('Galaxy S24', 'Samsung', 'Mobile', 'Smartphone', 3),
('ThinkPad X1', 'Lenovo', 'Laptop', 'Business', 1);

INSERT INTO public."Devices" ("ModelId", "Name", "Imei", "SerialNumber", "IsBorrowed") VALUES 
(1, 'iPhone 15 Black', 'IMEI001', 'SN001', FALSE),
(1, 'iPhone 15 White', 'IMEI002', 'SN002', FALSE),
(2, 'Galaxy S24 Ultra', 'IMEI003', 'SN003', FALSE),
(2, 'Galaxy S24 Plus', 'IMEI004', 'SN004', FALSE),
(2, 'Galaxy S24 Base', 'IMEI005', 'SN005', FALSE),
(3, 'ThinkPad X1 Carbon', 'IMEI006', 'SN006', FALSE);
