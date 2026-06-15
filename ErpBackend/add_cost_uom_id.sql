-- Add CostUomId column to Skus table for Cost Unit association
ALTER TABLE Skus ADD CostUomId UNIQUEIDENTIFIER NULL;

-- Add foreign key constraint
ALTER TABLE Skus ADD CONSTRAINT FK_Skus_CostUomId 
FOREIGN KEY (CostUomId) REFERENCES Uoms(Id);