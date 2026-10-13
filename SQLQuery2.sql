USE [期末];
GO

SELECT 
    c.ArtistName AS 演唱會名稱,
    cz.ZoneName AS 區域名稱,
    cz.Price AS 票價,
    cz.TotalSeats AS 總座位數,
    
    -- 1.【Subquery + Aggregate】用子查詢配合 COUNT 算出該區域賣出幾張票
    (SELECT COUNT(*) 
     FROM OrderDetails od 
     WHERE od.ZoneID = cz.ZoneID) AS 已售出張數,
     
    -- 2.【Subquery + Aggregate】動態算出剩餘座位
    (cz.TotalSeats - (SELECT COUNT(*) 
                      FROM OrderDetails od 
                      WHERE od.ZoneID = cz.ZoneID)) AS 剩餘座位數

-- 3.【Join】把區域表和演唱會主表串起來
FROM ConcertZones cz
JOIN Concerts c ON cz.ConcertID = c.ConcertID

ORDER BY c.ConcertID, cz.Price DESC;