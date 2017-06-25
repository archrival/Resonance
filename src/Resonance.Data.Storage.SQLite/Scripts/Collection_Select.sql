SELECT c.[Id],
	c.[Name],
	c.[Filter],
	c.[Path],
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified],
	s.[Enabled]
FROM [Collection] c
LEFT JOIN [History] ch ON ch.Id = c.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = c.Id AND mh.HistoryTypeId = 1
LEFT JOIN [Status] s ON s.Id = c.Id
GROUP BY c.Id
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL);