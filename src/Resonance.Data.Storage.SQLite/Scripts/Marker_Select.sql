SELECT
	m.*,
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified]
FROM [Marker] m
/**join**/
LEFT JOIN [History] ch ON ch.Id = m.TrackId AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = m.TrackId AND mh.HistoryTypeId = 1
/**where**/
GROUP BY m.TrackId, m.UserId
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
