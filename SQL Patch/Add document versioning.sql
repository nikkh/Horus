select fileName, ShreddingUtcDateTime, LatestVersionIndicator, DocumentVersion from Document order by fileName asc, ShreddingUtcDateTime desc

alter table document add DocumentVersion int
select documentnumber, ShreddingUtcDateTime, ROW_NUMBER() OVER (partition by DocumentNumber order by ShreddingUtcDateTime asc) as Row# from document
UPDATE document
SET DocumentVersion = calculatedVersion
FROM
(
	select id, ROW_NUMBER() OVER (partition by DocumentNumber order by ShreddingUtcDateTime asc) as calculatedVersion from document
) as blah
where document.Id = blah.id

alter table document add LatestVersionIndicator bit 
update document set LatestVersionIndicator = 'FALSE'
UPDATE 
    document
SET  
    LatestVersionIndicator = 'TRUE'
        
FROM  
    document d
    INNER JOIN
    (SELECT FileName, MAX(ShreddingUtcDateTime) AS MaxDateTime
    FROM Document 
    GROUP BY FileName) e
	ON d.FileName = e.FileName and 
	d.ShreddingUtcDateTime = e.MaxDateTime