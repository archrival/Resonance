SELECT
	p.*,
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified]
FROM [PlayQueue] p
/**join**/
LEFT JOIN [History] ch ON ch.Id = p.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = p.Id AND mh.HistoryTypeId = 1
/**where**/
GROUP BY p.Id
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
