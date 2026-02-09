-- Pharmacist Portal Tables
-- This file contains SQL for tables primarily used by the Pharmacist portal

-- ========================================
-- Patient Management Tables
-- ========================================

-- Patients Table
CREATE TABLE IF NOT EXISTS Patients (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    IdNumber VARCHAR(20) UNIQUE,
    PhoneNumber VARCHAR(100),
    Email VARCHAR(100),
    DateOfBirth DATE,
    Gender VARCHAR(10),
    Address VARCHAR(200),
    Allergies VARCHAR(100),
    MedicalHistory VARCHAR(500),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Patient Allergies Table (detailed allergy tracking)
CREATE TABLE IF NOT EXISTS PatientAllergies (
    Id SERIAL PRIMARY KEY,
    PatientId INTEGER NOT NULL REFERENCES Patients(Id) ON DELETE CASCADE,
    Allergen VARCHAR(100) NOT NULL,
    Severity VARCHAR(20) NOT NULL CHECK (Severity IN ('Mild', 'Moderate', 'Severe')),
    Reaction VARCHAR(200),
    Notes VARCHAR(500),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Patient Medical History Table (detailed medical history)
CREATE TABLE IF NOT EXISTS PatientMedicalHistory (
    Id SERIAL PRIMARY KEY,
    PatientId INTEGER NOT NULL REFERENCES Patients(Id) ON DELETE CASCADE,
    Condition VARCHAR(200) NOT NULL,
    DiagnosisDate DATE,
    Treatment VARCHAR(500),
    Medications VARCHAR(500),
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Resolved', 'Chronic')),
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Prescription Management Tables
-- ========================================

-- Prescriptions Table
CREATE TABLE IF NOT EXISTS Prescriptions (
    Id SERIAL PRIMARY KEY,
    RxNumber VARCHAR(50) NOT NULL UNIQUE,
    PatientId INTEGER NOT NULL REFERENCES Patients(Id) ON DELETE RESTRICT,
    PatientName VARCHAR(200) NOT NULL,
    PatientIdNumber VARCHAR(20),
    DoctorName VARCHAR(200) NOT NULL,
    DoctorRegistrationNumber VARCHAR(100),
    Medication VARCHAR(300) NOT NULL,
    Dosage VARCHAR(200) NOT NULL,
    Instructions VARCHAR(200) NOT NULL,
    TotalCost DECIMAL(10,2) NOT NULL CHECK (TotalCost >= 0),
    Status VARCHAR(20) DEFAULT 'pending' CHECK (Status IN ('pending', 'approved', 'filled', 'expired', 'cancelled')),
    PrescriptionDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ExpiryDate DATE,
    FilledDate DATE,
    Notes VARCHAR(500),
    IsUrgent BOOLEAN DEFAULT false,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Prescription Items Table (detailed prescription line items)
CREATE TABLE IF NOT EXISTS PrescriptionItems (
    Id SERIAL PRIMARY KEY,
    PrescriptionId INTEGER NOT NULL REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    InventoryItemId INTEGER NOT NULL REFERENCES InventoryItems(Id) ON DELETE RESTRICT,
    MedicationName VARCHAR(200) NOT NULL,
    GenericName VARCHAR(200),
    Dosage VARCHAR(100) NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    Instructions VARCHAR(200) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice >= 0),
    TotalPrice DECIMAL(10,2) NOT NULL CHECK (TotalPrice >= 0),
    DispensedQuantity INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Prescription Status History Table (tracking prescription status changes)
CREATE TABLE IF NOT EXISTS PrescriptionStatusHistory (
    Id SERIAL PRIMARY KEY,
    PrescriptionId INTEGER NOT NULL REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    PreviousStatus VARCHAR(20),
    NewStatus VARCHAR(20) NOT NULL,
    ChangedBy INTEGER REFERENCES Users(Id),
    Reason VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Pharmacy Inventory Tables
-- ========================================

-- Inventory Items Table (Zambia-specific pharmaceutical inventory)
CREATE TABLE IF NOT EXISTS InventoryItems (
    Id SERIAL PRIMARY KEY,
    InventoryItemName VARCHAR(200) NOT NULL,
    GenericName VARCHAR(200) NOT NULL,
    BrandName VARCHAR(200) NOT NULL,
    ManufactureDate DATE NOT NULL,
    ExpiryDate DATE,
    BatchNumber VARCHAR(100) NOT NULL UNIQUE,
    LicenseNumber VARCHAR(100),
    ZambiaRegNumber VARCHAR(100),
    PackingType VARCHAR(50) NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0 CHECK (Quantity >= 0),
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice >= 0),
    SellingPrice DECIMAL(10,2) NOT NULL CHECK (SellingPrice >= 0),
    ReorderLevel INTEGER DEFAULT 10,
    IsActive BOOLEAN DEFAULT true,
    RequiresPrescription BOOLEAN DEFAULT true,
    StorageConditions VARCHAR(200),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Drug Interactions Table (for checking medication interactions)
CREATE TABLE IF NOT EXISTS DrugInteractions (
    Id SERIAL PRIMARY KEY,
    Drug1Id INTEGER NOT NULL REFERENCES InventoryItems(Id) ON DELETE CASCADE,
    Drug2Id INTEGER NOT NULL REFERENCES InventoryItems(Id) ON DELETE CASCADE,
    InteractionType VARCHAR(50) NOT NULL, -- 'Minor', 'Moderate', 'Major', 'Contraindicated'
    Description TEXT NOT NULL,
    Recommendation VARCHAR(500),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Clinical Tables
-- ========================================

-- Doctor Information Table
CREATE TABLE IF NOT EXISTS Doctors (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    RegistrationNumber VARCHAR(100) UNIQUE,
    Specialization VARCHAR(100),
    PhoneNumber VARCHAR(20),
    Email VARCHAR(100),
    HospitalClinic VARCHAR(200),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Diagnosis Codes Table (ICD-10 or local coding system)
CREATE TABLE IF NOT EXISTS DiagnosisCodes (
    Id SERIAL PRIMARY KEY,
    Code VARCHAR(20) UNIQUE NOT NULL,
    Description VARCHAR(500) NOT NULL,
    Category VARCHAR(100),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Patient Visits Table (tracking patient visits)
CREATE TABLE IF NOT EXISTS PatientVisits (
    Id SERIAL PRIMARY KEY,
    PatientId INTEGER NOT NULL REFERENCES Patients(Id) ON DELETE CASCADE,
    DoctorId INTEGER REFERENCES Doctors(Id) ON DELETE SET NULL,
    VisitDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ReasonForVisit VARCHAR(500),
    Diagnosis VARCHAR(500),
    Treatment VARCHAR(500),
    Notes VARCHAR(1000),
    FollowUpDate DATE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Regulatory and Compliance Tables
-- ========================================

-- Regulatory Compliance Table (Zambia pharmacy regulations)
CREATE TABLE IF NOT EXISTS RegulatoryCompliance (
    Id SERIAL PRIMARY KEY,
    ComplianceType VARCHAR(100) NOT NULL, -- 'License', 'Inspection', 'Training', etc.
    ReferenceNumber VARCHAR(100),
    IssuingAuthority VARCHAR(200),
    IssueDate DATE,
    ExpiryDate DATE,
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Expired', 'Suspended', 'Revoked')),
    DocumentPath VARCHAR(500),
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Controlled Substances Log Table
CREATE TABLE IF NOT EXISTS ControlledSubstancesLog (
    Id SERIAL PRIMARY KEY,
    InventoryItemId INTEGER NOT NULL REFERENCES InventoryItems(Id) ON DELETE RESTRICT,
    TransactionType VARCHAR(50) NOT NULL CHECK (TransactionType IN ('Received', 'Dispensed', 'Returned', 'Destroyed')),
    Quantity INTEGER NOT NULL,
    Balance INTEGER NOT NULL,
    PrescriptionId INTEGER REFERENCES Prescriptions(Id),
    AuthorizedBy INTEGER REFERENCES Users(Id),
    WitnessName VARCHAR(200),
    Reason VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Pharmacist Portal Tables
-- ========================================

-- Patients Indexes
CREATE INDEX IF NOT EXISTS idx_patients_id_number ON Patients(IdNumber);
CREATE INDEX IF NOT EXISTS idx_patients_name ON Patients(Name);
CREATE INDEX IF NOT EXISTS idx_patients_phone ON Patients(PhoneNumber);
CREATE INDEX IF NOT EXISTS idx_patients_active ON Patients(IsActive);

-- Patient Allergies Indexes
CREATE INDEX IF NOT EXISTS idx_patientallergies_patient ON PatientAllergies(PatientId);
CREATE INDEX IF NOT EXISTS idx_patientallergies_allergen ON PatientAllergies(Allergen);
CREATE INDEX IF NOT EXISTS idx_patientallergies_active ON PatientAllergies(IsActive);

-- Patient Medical History Indexes
CREATE INDEX IF NOT EXISTS idx_patientmedicalhistory_patient ON PatientMedicalHistory(PatientId);
CREATE INDEX IF NOT EXISTS idx_patientmedicalhistory_condition ON PatientMedicalHistory(Condition);
CREATE INDEX IF NOT EXISTS idx_patientmedicalhistory_status ON PatientMedicalHistory(Status);

-- Prescriptions Indexes
CREATE INDEX IF NOT EXISTS idx_prescriptions_rx ON Prescriptions(RxNumber);
CREATE INDEX IF NOT EXISTS idx_prescriptions_patient ON Prescriptions(PatientId);
CREATE INDEX IF NOT EXISTS idx_prescriptions_doctor ON Prescriptions(DoctorName);
CREATE INDEX IF NOT EXISTS idx_prescriptions_status ON Prescriptions(Status);
CREATE INDEX IF NOT EXISTS idx_prescriptions_date ON Prescriptions(PrescriptionDate);
CREATE INDEX IF NOT EXISTS idx_prescriptions_urgent ON Prescriptions(IsUrgent);

-- Prescription Items Indexes
CREATE INDEX IF NOT EXISTS idx_prescriptionitems_prescription ON PrescriptionItems(PrescriptionId);
CREATE INDEX IF NOT EXISTS idx_prescriptionitems_inventory ON PrescriptionItems(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_prescriptionitems_medication ON PrescriptionItems(MedicationName);

-- Prescription Status History Indexes
CREATE INDEX IF NOT EXISTS idx_prescriptionstatushistory_prescription ON PrescriptionStatusHistory(PrescriptionId);
CREATE INDEX IF NOT EXISTS idx_prescriptionstatushistory_date ON PrescriptionStatusHistory(CreatedAt);

-- Inventory Items Indexes
CREATE INDEX IF NOT EXISTS idx_inventoryitems_batch ON InventoryItems(BatchNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_zambia_reg ON InventoryItems(ZambiaRegNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_generic ON InventoryItems(GenericName);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_brand ON InventoryItems(BrandName);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_expiry ON InventoryItems(ExpiryDate);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_active ON InventoryItems(IsActive);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_prescription_required ON InventoryItems(RequiresPrescription);

-- Drug Interactions Indexes
CREATE INDEX IF NOT EXISTS idx_druginteractions_drug1 ON DrugInteractions(Drug1Id);
CREATE INDEX IF NOT EXISTS idx_druginteractions_drug2 ON DrugInteractions(Drug2Id);
CREATE INDEX IF NOT EXISTS idx_druginteractions_type ON DrugInteractions(InteractionType);

-- Doctors Indexes
CREATE INDEX IF NOT EXISTS idx_doctors_name ON Doctors(Name);
CREATE INDEX IF NOT EXISTS idx_doctors_registration ON Doctors(RegistrationNumber);
CREATE INDEX IF NOT EXISTS idx_doctors_specialization ON Doctors(Specialization);
CREATE INDEX IF NOT EXISTS idx_doctors_active ON Doctors(IsActive);

-- Patient Visits Indexes
CREATE INDEX IF NOT EXISTS idx_patientvisits_patient ON PatientVisits(PatientId);
CREATE INDEX IF NOT EXISTS idx_patientvisits_doctor ON PatientVisits(DoctorId);
CREATE INDEX IF NOT EXISTS idx_patientvisits_date ON PatientVisits(VisitDate);

-- Regulatory Compliance Indexes
CREATE INDEX IF NOT EXISTS idx_regulatorycompliance_type ON RegulatoryCompliance(ComplianceType);
CREATE INDEX IF NOT EXISTS idx_regulatorycompliance_status ON RegulatoryCompliance(Status);
CREATE INDEX IF NOT EXISTS idx_regulatorycompliance_expiry ON RegulatoryCompliance(ExpiryDate);

-- Controlled Substances Log Indexes
CREATE INDEX IF NOT EXISTS idx_controlledsubstanceslog_inventory ON ControlledSubstancesLog(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_controlledsubstanceslog_prescription ON ControlledSubstancesLog(PrescriptionId);
CREATE INDEX IF NOT EXISTS idx_controlledsubstanceslog_date ON ControlledSubstancesLog(CreatedAt);

-- ========================================
-- Triggers for Pharmacist Portal Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to Pharmacist tables
CREATE TRIGGER update_patients_updated_at BEFORE UPDATE ON Patients 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_patientallergies_updated_at BEFORE UPDATE ON PatientAllergies 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_patientmedicalhistory_updated_at BEFORE UPDATE ON PatientMedicalHistory 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_prescriptions_updated_at BEFORE UPDATE ON Prescriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_inventoryitems_updated_at BEFORE UPDATE ON InventoryItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_doctors_updated_at BEFORE UPDATE ON Doctors 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_regulatorycompliance_updated_at BEFORE UPDATE ON RegulatoryCompliance 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger to log prescription status changes
CREATE OR REPLACE FUNCTION log_prescription_status_change()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.Status IS DISTINCT FROM NEW.Status THEN
        INSERT INTO PrescriptionStatusHistory (PrescriptionId, PreviousStatus, NewStatus, ChangedBy, Reason)
        VALUES (NEW.Id, OLD.Status, NEW.Status, NEW.UserId, 'Status updated');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER prescription_status_change_trigger
    AFTER UPDATE ON Prescriptions
    FOR EACH ROW EXECUTE FUNCTION log_prescription_status_change();

-- ========================================
-- Views for Pharmacist Dashboard
-- ========================================

-- View for Patient Summary
CREATE OR REPLACE VIEW PatientSummary AS
SELECT 
    p.Id,
    p.Name,
    p.IdNumber,
    p.PhoneNumber,
    p.DateOfBirth,
    p.Gender,
    p.IsActive,
    COUNT(DISTINCT pr.Id) as TotalPrescriptions,
    COUNT(DISTINCT CASE WHEN pr.Status = 'pending' THEN pr.Id END) as PendingPrescriptions,
    COUNT(DISTINCT CASE WHEN pr.Status = 'filled' THEN pr.Id END) as FilledPrescriptions,
    COUNT(DISTINCT pa.Id) as Allergies,
    p.CreatedAt
FROM Patients p
LEFT JOIN Prescriptions pr ON p.Id = pr.PatientId
LEFT JOIN PatientAllergies pa ON p.Id = pa.PatientId AND pa.IsActive = true
GROUP BY p.Id, p.Name, p.IdNumber, p.PhoneNumber, p.DateOfBirth, p.Gender, p.IsActive, p.CreatedAt;

-- View for Prescription Summary
CREATE OR REPLACE VIEW PrescriptionSummary AS
SELECT 
    pr.Id,
    pr.RxNumber,
    pr.PatientName,
    pr.DoctorName,
    pr.Medication,
    pr.Status,
    pr.PrescriptionDate,
    pr.ExpiryDate,
    pr.IsUrgent,
    pr.TotalCost,
    COUNT(DISTINCT pri.Id) as ItemCount,
    SUM(pri.Quantity) as TotalQuantity,
    pr.CreatedAt
FROM Prescriptions pr
LEFT JOIN PrescriptionItems pri ON pr.Id = pri.PrescriptionId
GROUP BY pr.Id, pr.RxNumber, pr.PatientName, pr.DoctorName, pr.Medication, pr.Status, 
         pr.PrescriptionDate, pr.ExpiryDate, pr.IsUrgent, pr.TotalCost, pr.CreatedAt;

-- View for Inventory Status
CREATE OR REPLACE VIEW InventoryStatus AS
SELECT 
    ii.Id,
    ii.InventoryItemName,
    ii.GenericName,
    ii.BrandName,
    ii.BatchNumber,
    ii.ZambiaRegNumber,
    ii.Quantity,
    ii.ReorderLevel,
    ii.UnitPrice,
    ii.SellingPrice,
    ii.ExpiryDate,
    ii.RequiresPrescription,
    CASE 
        WHEN ii.ExpiryDate < CURRENT_DATE THEN 'Expired'
        WHEN ii.ExpiryDate <= CURRENT_DATE + INTERVAL '30 days' THEN 'Expiring Soon'
        WHEN ii.Quantity <= ii.ReorderLevel THEN 'Low Stock'
        ELSE 'Normal'
    END as Status,
    ii.IsActive
FROM InventoryItems ii
WHERE ii.IsActive = true;

-- View for Drug Interaction Alerts
CREATE OR REPLACE VIEW DrugInteractionAlerts AS
SELECT 
    di.Id,
    drug1.InventoryItemName as Drug1Name,
    drug2.InventoryItemName as Drug2Name,
    di.InteractionType,
    di.Description,
    di.Recommendation
FROM DrugInteractions di
JOIN InventoryItems drug1 ON di.Drug1Id = drug1.Id
JOIN InventoryItems drug2 ON di.Drug2Id = drug2.Id
WHERE di.IsActive = true
  AND drug1.IsActive = true
  AND drug2.IsActive = true;
