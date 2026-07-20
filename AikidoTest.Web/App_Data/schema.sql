-- Sample schema for the AikidoTest vulnerable demo app.
-- Note the Orders table stores a plaintext PAN and CVV column, which by
-- itself is a PCI-DSS violation regardless of the application code.

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(64) NOT NULL,
    IsAdmin BIT NOT NULL DEFAULT 0
);

CREATE TABLE Products (
    ProductId INT IDENTITY PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL
);

CREATE TABLE Orders (
    OrderId INT IDENTITY PRIMARY KEY,
    CardHolderName NVARCHAR(200) NOT NULL,
    CardNumber NVARCHAR(25) NOT NULL,          -- PCI-DSS: plaintext PAN at rest
    CardNumberEncrypted NVARCHAR(200) NULL,    -- "encrypted" with weak DES, see CryptoHelper
    Cvv NVARCHAR(4) NOT NULL,                  -- PCI-DSS: CVV must never be stored
    Amount DECIMAL(10,2) NOT NULL,
    CreatedUtc DATETIME NOT NULL DEFAULT GETUTCDATE()
);

INSERT INTO Users (Username, PasswordHash, IsAdmin) VALUES
    ('admin', '21232f297a57a5a743894a0e4a801fc3', 1); -- MD5('admin')
