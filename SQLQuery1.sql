USE [期末]; -- 👈 如果你的資料庫名稱是 [期末]，請自行確認
GO

-- ==========================================
-- 1. 依照外鍵相依順序，由下往上刪除舊資料表
-- ==========================================
DROP TABLE IF EXISTS OrderDetails;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Members;
DROP TABLE IF EXISTS ConcertZones;
DROP TABLE IF EXISTS Concerts;
GO

-- ==========================================
-- 2. 建立全新結構資料表
-- ==========================================
CREATE TABLE Concerts (
    ConcertID VARCHAR(10) PRIMARY KEY,
    ArtistName NVARCHAR(50) NOT NULL,
    Venue NVARCHAR(50) NOT NULL
);

CREATE TABLE ConcertZones (
    ZoneID INT IDENTITY(1,1) PRIMARY KEY,
    ConcertID VARCHAR(10) NOT NULL,
    ZoneName NVARCHAR(30) NOT NULL, -- 👈 稍微加大長度以容納「價位 - 區域」的字數
    Price INT NOT NULL,
    TotalSeats INT NOT NULL,
    FOREIGN KEY (ConcertID) REFERENCES Concerts(ConcertID)
);

CREATE TABLE Members (
    MemberID VARCHAR(10) PRIMARY KEY,
    MemberName NVARCHAR(20) NOT NULL,
    Password VARCHAR(50) NOT NULL
);

CREATE TABLE Orders (
    OrderID VARCHAR(20) PRIMARY KEY,  
    MemberID VARCHAR(10) NOT NULL,
    TotalTickets INT NOT NULL CHECK (TotalTickets <= 4), 
    OrderDate DATETIME NOT NULL,
    FOREIGN KEY (MemberID) REFERENCES Members(MemberID)
);

CREATE TABLE OrderDetails (
    DetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID VARCHAR(20) NOT NULL,     
    ZoneID INT NOT NULL,
    SeatNumber VARCHAR(10) NOT NULL,
    RealName NVARCHAR(20) NOT NULL,       
    IDNumber VARCHAR(10) NOT NULL,        
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ZoneID) REFERENCES ConcertZones(ZoneID)
);
GO

-- ==========================================
-- 3. 插入 5 大熱門場次
-- ==========================================
INSERT INTO Concerts (ConcertID, ArtistName, Venue) VALUES 
('AESPA01', N'aespa', N'台北大巨蛋'),
('TWICE02', N'TWICE', N'高雄國家體育場'),
('ITZY03', N'ITZY', N'台北流行音樂中心'),
('NMIXX04', N'NMIXX', N'林口體育館'),
('BABYMON05', N'BABYMONSTER', N'桃園巨蛋 (桃園市立體育館)');
GO

-- ==========================================
-- 4. 插入標準化三位數樓層區域 (每場 6個票價 / 15個區域)
-- ==========================================

-- (1) aespa 台北大巨蛋 (價位: 6800, 5800, 4800, 3800, 2800, 1800)
INSERT INTO ConcertZones (ConcertID, ZoneName, Price, TotalSeats) VALUES 
('AESPA01', N'6800 - 101區', 6800, 15),
('AESPA01', N'6800 - 102區', 6800, 20),
('AESPA01', N'5800 - 103區', 5800, 30),
('AESPA01', N'5800 - 104區', 5800, 35),
('AESPA01', N'5800 - 105區', 5800, 40),
('AESPA01', N'4800 - 201區', 4800, 80),
('AESPA01', N'4800 - 202區', 4800, 85),
('AESPA01', N'4800 - 203區', 4800, 90),
('AESPA01', N'3800 - 204區', 3800, 100),
('AESPA01', N'3800 - 205區', 3800, 110),
('AESPA01', N'2800 - 301區', 2800, 120),
('AESPA01', N'2800 - 302區', 2800, 125),
('AESPA01', N'1800 - 401區', 1800, 150),
('AESPA01', N'1800 - 402區', 1800, 160),
('AESPA01', N'1800 - 403區', 1800, 50);

-- (2) TWICE 高雄國家體育場 (價位: 5800, 4800, 3800, 2800, 1800, 800)
INSERT INTO ConcertZones (ConcertID, ZoneName, Price, TotalSeats) VALUES 
('TWICE02', N'5800 - 101區', 5800, 12),
('TWICE02', N'5800 - 102區', 5800, 18),
('TWICE02', N'4800 - 103區', 4800, 35),
('TWICE02', N'4800 - 104區', 4800, 40),
('TWICE02', N'4800 - 105區', 4800, 40),
('TWICE02', N'3800 - 201區', 3800, 75),
('TWICE02', N'3800 - 202區', 3800, 75),
('TWICE02', N'2800 - 203區', 2800, 110),
('TWICE02', N'2800 - 204區', 2800, 115),
('TWICE02', N'1800 - 301區', 1800, 140),
('TWICE02', N'1800 - 302區', 1800, 140),
('TWICE02', N'1800 - 303區', 1800, 150),
('TWICE02', N'800 - 401區', 800, 200),
('TWICE02', N'800 - 402區', 800, 200),
('TWICE02', N'800 - 403區', 800, 90);

-- (3) ITZY 台北流行音樂中心 (價位: 4800, 4200, 3600, 3000, 2200, 1600)
INSERT INTO ConcertZones (ConcertID, ZoneName, Price, TotalSeats) VALUES 
('ITZY03', N'4800 - 101區', 4800, 15),
('ITZY03', N'4800 - 102區', 4800, 20),
('ITZY03', N'4200 - 103區', 4200, 30),
('ITZY03', N'4200 - 104區', 4200, 35),
('ITZY03', N'3600 - 201區', 3600, 50),
('ITZY03', N'3600 - 202區', 3600, 55),
('ITZY03', N'3000 - 203區', 3000, 60),
('ITZY03', N'3000 - 204區', 3000, 65),
('ITZY03', N'2200 - 301區', 2200, 80),
('ITZY03', N'2200 - 302區', 2200, 80),
('ITZY03', N'1600 - 303區', 1600, 100),
('ITZY03', N'1600 - 304區', 1600, 100),
('ITZY03', N'1600 - 401區', 1600, 90),
('ITZY03', N'1600 - 402區', 1600, 85),
('ITZY03', N'1600 - 403區', 1600, 10);

-- (4) NMIXX 林口體育館 (價位: 4600, 4000, 3400, 2800, 2200, 1200)
INSERT INTO ConcertZones (ConcertID, ZoneName, Price, TotalSeats) VALUES 
('NMIXX04', N'4600 - 101區', 4600, 20),
('NMIXX04', N'4600 - 102區', 4600, 25),
('NMIXX04', N'4000 - 103區', 4000, 40),
('NMIXX04', N'4000 - 104區', 4000, 45),
('NMIXX04', N'3400 - 201區', 3400, 60),
('NMIXX04', N'3400 - 202區', 3400, 65),
('NMIXX04', N'2800 - 203區', 2800, 80),
('NMIXX04', N'2800 - 204區', 2800, 80),
('NMIXX04', N'2800 - 205區', 2800, 75),
('NMIXX04', N'2200 - 301區', 2200, 110),
('NMIXX04', N'2200 - 302區', 2200, 115),
('NMIXX04', N'1200 - 303區', 1200, 130),
('NMIXX04', N'1200 - 401區', 1200, 140),
('NMIXX04', N'1200 - 402區', 1200, 120),
('NMIXX04', N'1200 - 403區', 1200, 40);

-- (5) BABYMONSTER 桃園巨蛋 (價位: 5800, 4800, 3800, 2800, 1800, 1000)
INSERT INTO ConcertZones (ConcertID, ZoneName, Price, TotalSeats) VALUES 
('BABYMON05', N'5800 - 101區', 5800, 15),
('BABYMON05', N'5800 - 102區', 5800, 25),
('BABYMON05', N'4800 - 103區', 4800, 35),
('BABYMON05', N'4800 - 104區', 4800, 40),
('BABYMON05', N'3800 - 201區', 3800, 60),
('BABYMON05', N'3800 - 202區', 3800, 70),
('BABYMON05', N'3800 - 203區', 3800, 70),
('BABYMON05', N'2800 - 204區', 2800, 90),
('BABYMON05', N'2800 - 205區', 2800, 95),
('BABYMON05', N'2800 - 301區', 2800, 85),
('BABYMON05', N'1800 - 302區', 1800, 120),
('BABYMON05', N'1800 - 303區', 1800, 125),
('BABYMON05', N'1800 - 401區', 1800, 110),
('BABYMON05', N'1000 - 402區', 1000, 150),
('BABYMON05', N'1000 - 403區', 1000, 45);
GO

-- ==========================================
-- 5. 會員與測試訂單初始化 (密碼 1234)
-- ==========================================
INSERT INTO Members (MemberID, MemberName, Password) VALUES 
('M001', N'張小明', '1234'),
('M002', N'王大同', '1234');

INSERT INTO Orders (OrderID, MemberID, TotalTickets, OrderDate) VALUES 
('ORD202601', 'M001', 2, '2026-06-22 12:00:03');

-- 對應至 aespa 的第一區 (ZoneID = 1 即 6800 - 101區)
INSERT INTO OrderDetails (OrderID, ZoneID, SeatNumber, RealName, IDNumber) VALUES 
('ORD202601', 1, 'A排01號', N'張小明', 'A123456789'), 
('ORD202601', 1, 'A排02號', N'陳大文', 'B223456789');
GO