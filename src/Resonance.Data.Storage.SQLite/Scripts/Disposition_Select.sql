SELECT
	d.Id AS [DispositionId], 
	d.CollectionId,
	d.MediaTypeId, 
	d.Favorited, 
	d.MediaId, 
	d.UserId, 
	d.Rating, 
	ad.AverageRating
FROM [Disposition] d
/**join**/
LEFT JOIN (SELECT MediaId, AVG(Rating) AS [AverageRating] FROM Disposition) AS ad ON ad.MediaId = d.MediaId
/**where**/
/**orderby**/
