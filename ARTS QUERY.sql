

create database ARTS

use ARTS;

CREATE TABLE Category (
    CategoryID INT PRIMARY KEY IDENTITY(1,1), -- Auto-increment ID
    CategoryName VARCHAR(100) NOT NULL,
	CategoryImage VARCHAR(max),
    Description TEXT
);

select * from Category


CREATE TABLE Products (
    ProductID CHAR(7) PRIMARY KEY,           -- 7 digit product id (2 digit product code + 5 digit product number)
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
	ImagePath NVARCHAR(MAX) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL CHECK (Price >= 0),
    Category_id INT NOT NULL,
	CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    WarrantyPeriodDays INT NULL,             -- warranty period in days
    CONSTRAINT FK_Products_Categories FOREIGN KEY (Category_id) REFERENCES Category(CategoryID)
);


--ALTER TABLE Products
--ADD CreatedDate DATETIME NOT NULL DEFAULT GETDATE();





SELECT 
    P.ProductID,
    P.ProductName,
    P.Description,
	P.ImagePath,
    P.Price,
    P.StockQuantity,
    P.WarrantyPeriodDays,
	p.CreatedDate,
    C.CategoryName
FROM Products P
JOIN Category C ON P.Category_id = C.CategoryID;
















CREATE TABLE ProductReviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId CHAR(7) NOT NULL,  -- Match Products.ProductID
    ReviewerName NVARCHAR(MAX) NOT NULL,
    ReviewerEmail NVARCHAR(MAX) NOT NULL,
    ReviewContent NVARCHAR(MAX) NOT NULL,
    Rating INT NOT NULL,
    ReviewDate DATE,
    CONSTRAINT FK_ProductReviews_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductID)
);
SELECT 
    pr.ReviewID,
    p.ProductName,
    p.Price,
    pr.ReviewerName,
    pr.ReviewContent,
    pr.Rating,
    pr.ReviewDate
FROM 
    ProductReviews pr
JOIN 
    Products p ON pr.ProductID = p.ProductID;





















CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserEmail NVARCHAR(256) NOT NULL UNIQUE,
    UserName NVARCHAR(100) NOT NULL,
	ImagePath NVARCHAR(300) NULL,
    UserPassword NVARCHAR(256) NOT NULL,
    Role NVARCHAR(50) NOT NULL,  -- 'Admin', 'Employee', 'Customer'
    Address NVARCHAR(250) NULL,
	 RegistrationDate DATETIME NOT NULL DEFAULT GETDATE(),
    PhoneNumber NVARCHAR(20) NULL
);
INSERT INTO Users (UserEmail, UserName, UserPassword, Role, Address, PhoneNumber)
VALUES (
    'admin@gmail.com',
    'Admin',
    'Admin123',
    'Admin',
    NULL,
    NULL
  
);

--ALTER TABLE Users
--ADD ImagePath NVARCHAR(300) NULL;

SELECT * FROM Users

UPDATE Users
SET ImagePath = '~/frontassets/img/person/profile.png'
WHERE Role = 'Admin' AND (ImagePath IS NULL OR ImagePath = '~/frontassets/img/person/profile.png');

UPDATE Users
SET ImagePath = '~/frontassets/img/person/profile.png'
WHERE Role = 'Employee' AND (ImagePath IS NULL OR ImagePath = '~/frontassets/img/person/profile.png');

UPDATE Users
SET ImagePath = '~/frontassets/img/person/profile.png'
WHERE Role = 'Customer' AND (ImagePath IS NULL OR ImagePath = '~/frontassets/img/person/profile.png');


UPDATE Users
SET UserPassword = 'Admin123'
WHERE UserEmail = 'admin@gmail.com';















CREATE TABLE ConfirmEmployee (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [From] NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Subject NVARCHAR(255) NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    EmployeeId NVARCHAR(50) NOT NULL,
    Message NVARCHAR(MAX) NULL,
    SentAt DATETIME DEFAULT GETDATE()
);


CREATE TABLE Cart (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,                       -- customer ka reference
    ProductID CHAR(7) NOT NULL,                -- product ka reference
    Quantity INT NOT NULL DEFAULT 1,           -- kitne quantity order ki
    AddedDate DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Cart_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Cart_Product FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

select * from Cart


CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1), -- Auto-incrementing primary key
    OrderNumber VARCHAR(16) UNIQUE NOT NULL, -- 16-digit unique order number (1-digit delivery type + 7-digit product ID + 8-digit order number)
    UserId INT NOT NULL, -- Foreign key to Users table (customer who placed the order)
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(), -- Date and time when the order was placed
    Subtotal DECIMAL(18,2) NOT NULL, -- Total cost of items before tax and shipping
    Shipping DECIMAL(18,2) NOT NULL, -- Shipping cost
    Tax DECIMAL(18,2) NOT NULL, -- Tax amount (e.g., 5% of subtotal)
    Total DECIMAL(18,2) NOT NULL, -- Total amount (subtotal + shipping + tax)
    PaymentMethod VARCHAR(50) NOT NULL, -- e.g., 'cod', 'card', 'bank'
    PaymentStatus VARCHAR(50) NOT NULL DEFAULT 'Pending', -- e.g., 'Pending', 'Cleared', 'PaidOnDelivery'
    DeliveryType CHAR(1) NOT NULL, -- e.g., '1' for standard, '2' for express
    EstimatedDeliveryDate DATETIME NOT NULL, -- Estimated delivery date
    DispatchedDate DATETIME NULL, -- Date when the order was dispatched
    DeliveryDate DATETIME NULL, -- Date when the order was delivered
    OrderStatus VARCHAR(50) NOT NULL DEFAULT 'Pending', -- e.g., 'Pending', 'Processing', 'Dispatched', 'Delivered', 'Canceled', 'Returned'
    FullName VARCHAR(100) NOT NULL, -- Customer's full name
    Phone VARCHAR(15) NOT NULL, -- Customer's phone number
    Address NVARCHAR(500) NOT NULL, -- Complete address
    City VARCHAR(100) NOT NULL, -- City
    State VARCHAR(100) NOT NULL, -- State/Province
    ZipCode VARCHAR(20) NOT NULL, -- ZIP/Postal code
	CardNumber VARCHAR(20) NULL,
    BankAccountNumber VARCHAR(20) NULL,
	CardHolderName VARCHAR(20) NULL,
    BankName VARCHAR(20) NULL,
    TransactionId VARCHAR(50) NULL,
	ExpiryDate VARCHAR(5) NULL,
    CVC VARCHAR(4) NULL,
	ReturnReason NVARCHAR(MAX) NULL,
    ReplacementReason NVARCHAR(MAX) NULL,
	RefundEasypaisaNumber VARCHAR(11) NULL,
    RefundBankAccountNumber VARCHAR(16) NULL,
    ReturnRequested BIT NOT NULL DEFAULT 0, -- Flag for return request
    ReturnStatus VARCHAR(50) NULL, -- e.g., 'Pending', 'Approved', 'Rejected'
    ReplacementRequested BIT NOT NULL DEFAULT 0, -- Flag for replacement request
    ReplacementStatus VARCHAR(50) NULL, -- e.g., 'Pending', 'Approved', 'Rejected'
    CreatedBy INT NULL, -- Foreign key to Users table (employee/admin who created/processed the order)
    UpdatedBy INT NULL, -- Foreign key to Users table (employee/admin who last updated the order)
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Record creation date
    UpdatedDate DATETIME NULL, -- Last update date
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    FOREIGN KEY (UpdatedBy) REFERENCES Users(UserId)
);


ALTER TABLE Orders
ADD 	ReturnReason NVARCHAR(MAX) NULL,
    ReplacementReason NVARCHAR(MAX) NULL;



SELECT * FROM Orders

CREATE TABLE OrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1), -- Auto-incrementing primary key
    OrderId INT NOT NULL, -- Foreign key to Orders table
    ProductId CHAR(7) NOT NULL, -- 7-digit product ID (2-digit code + 5-digit number)
    Quantity INT NOT NULL, -- Quantity of the product ordered
    UnitPrice DECIMAL(18,2) NOT NULL, -- Price per unit at the time of order
    WarrantyPeriodDays INT NULL, -- Warranty period for the product (if applicable)
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);
SELECT * FROM OrderItems













SELECT * FROM Users
SELECT * FROM Products

CREATE TABLE FAQCategories (
    FCategoryId INT IDENTITY(1,1) PRIMARY KEY,
    FCategory VARCHAR(100) NOT NULL
);
select * from FAQCategories;


CREATE TABLE FAQs (
    FAQId INT IDENTITY(1,1) PRIMARY KEY,
    FQuestion NVARCHAR(500) NOT NULL,
    FAnswer NVARCHAR(MAX) NOT NULL,
    FCategoryId INT NOT NULL,
	  CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (FCategoryId) REFERENCES FAQCategories(FCategoryId) ON DELETE CASCADE
);
select * from FAQs;

CREATE TABLE ContactUs (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    ContactName NVARCHAR(100) NOT NULL,
    ContactEmail NVARCHAR(100) NOT NULL,
    ContactSubject NVARCHAR(200) NULL,
    ContactMessage NVARCHAR(MAX) NOT NULL
);



select * from ContactUs
select * from Users
select * from FAQs;
select * from FAQCategories;
SELECT * FROM Products
SELECT * FROM Orders
SELECT * FROM OrderItems
select * from Cart




