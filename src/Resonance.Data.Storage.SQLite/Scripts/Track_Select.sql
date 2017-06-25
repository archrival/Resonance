SELECT
	  t.Id,
	  t.Bitrate,
	  t.CollectionId,
	  t.ContentType,
	  t.Disc AS [DiscNumber],
	  t.Duration,
	  t.Name,
 	  t.Number,
	  t.Path,
	  t.ReleaseDate,
	  fi.DateCreated AS [DateFileCreated],
	  fi.DateModified AS [DateFileModified],
	  fi.Size,
	  fi.Visible,
	  ch.[Timestamp] AS [DateAdded],
	  mh.[Timestamp] AS [DateModified],
	  d.MediaTypeId, 
	  d.Favorited, 
	  d.Id AS [DispositionId], 
	  d.Rating,
	  AVG(ad.Rating) AS [AverageRating],
	  tta.AlbumId
/**addselect**/
FROM [Track] t
JOIN [FileInfo] fi ON fi.TrackId = t.Id
JOIN [TrackToAlbum] tta ON tta.TrackId = t.Id
/**join**/
LEFT JOIN [Disposition] d ON (d.MediaId = t.Id AND d.UserId = @UserId)
LEFT JOIN [Disposition] ad ON ad.MediaId = t.Id 
LEFT JOIN [History] ch ON ch.Id = t.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = t.Id AND mh.HistoryTypeId = 1
/**where**/
GROUP BY t.Id, ad.MediaId
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
