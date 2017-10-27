SELECT
	r.*,
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified]
FROM [RadioStation] r
/**join**/
LEFT JOIN [History] ch ON ch.Id = r.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = r.Id AND mh.HistoryTypeId = 1
/**where**/
GROUP BY r.Id
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
