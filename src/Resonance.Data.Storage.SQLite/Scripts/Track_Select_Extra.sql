SELECT
	  t.Channels,
	  t.Comment,
	  t.SampleRate,
	  h.Hash AS [FileHash],
	  h.HashTypeId,
	  rg.AlbumGain,
	  rg.AlbumPeak,
	  rg.TrackGain,
	  rg.TrackPeak
/**addselect**/
FROM [Track] t
JOIN [FileInfo] fi ON fi.TrackId = t.Id
/**join**/
LEFT JOIN [Hash] h ON h.TrackId = fi.TrackId
LEFT JOIN [MediaInfo] mi ON mi.MediaId = t.Id
LEFT JOIN [LastFm] lf ON lf.Id = mi.LastFmId 
LEFT JOIN [ReplayGain] rg ON rg.TrackId = t.Id
/**where**/
GROUP BY t.Id
/**orderby**/
/**limit**/
