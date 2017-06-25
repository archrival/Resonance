SELECT
	g.Name AS [Genre],
	COUNT(gtt.TrackId) AS [SongCount],
	COUNT(DISTINCT tta.AlbumId) AS [AlbumCount]
FROM [Genre] g
LEFT JOIN [GenreToTrack] gtt ON gtt.GenreId = g.Id
LEFT JOIN [TrackToAlbum] tta ON tta.TrackId = gtt.TrackId
WHERE gtt.GenreId IS NOT NULL
GROUP BY g.Name
