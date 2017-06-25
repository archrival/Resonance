SELECT g.*,
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified]
FROM [Genre] g
/**join**/
/**leftjoin**/
LEFT JOIN [History] ch ON ch.Id = g.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = g.Id AND mh.HistoryTypeId = 1
/**where**/
GROUP BY g.Id
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
