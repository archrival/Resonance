SELECT 
	a.*,
	d.MediaTypeId,
	d.Favorited,
	d.Id AS [DispositionId],
	d.Rating, 
	AVG(ad.Rating) AS [AverageRating],
	ch.[Timestamp] AS [DateAdded],
	mh.[Timestamp] AS [DateModified]
FROM [Artist] a
/**join**/
/**leftjoin**/
LEFT JOIN [Disposition] d ON (d.MediaId = a.Id AND d.UserId = @UserId)
LEFT JOIN [Disposition] ad ON ad.MediaId = a.Id
LEFT JOIN [History] ch ON ch.Id = a.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = a.Id AND mh.HistoryTypeId = 1
/**where**/
GROUP BY a.Id, ad.MediaId
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
/**offset**/
