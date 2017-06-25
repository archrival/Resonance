SELECT
	mi.*,
	lf.Url,
	lf.Details,
	lf.SmallImageUrl,
	lf.MediumImageUrl,
	lf.LargeImageUrl,
	lf.LargestImageUrl,
	mch.[Timestamp] AS [MediaInfoDateAdded],
	mmh.[Timestamp] AS [MediaInfoDateModified],
	lch.[Timestamp] AS [LastFmDateAdded],
	lmh.[Timestamp] AS [LastFmDateModified]
FROM [MediaInfo] mi
/**join**/
LEFT JOIN [LastFm] lf ON (lf.Id = mi.LastFmId OR lf.MusicBrainzId = mi.MusicBrainzId)
LEFT JOIN [History] mch ON mch.Id = mi.Id AND mch.HistoryTypeId = 0
LEFT JOIN [History] mmh ON mmh.Id = mi.Id AND mmh.HistoryTypeId = 1
LEFT JOIN [History] lch ON lch.Id = lf.Id AND lch.HistoryTypeId = 0
LEFT JOIN [History] lmh ON lmh.Id = lf.Id AND lmh.HistoryTypeId = 1
/**where**/
GROUP BY mi.MediaId
HAVING (MAX(mmh.[Timestamp]) OR mmh.[Timestamp] IS NULL) OR (MIN(mch.[Timestamp]) OR mch.[Timestamp] IS NULL) OR (MAX(lmh.[Timestamp]) OR lmh.[Timestamp] IS NULL) OR (MIN(lch.[Timestamp]) OR lch.[Timestamp] IS NULL)
/**orderby**/
/**limit**/
