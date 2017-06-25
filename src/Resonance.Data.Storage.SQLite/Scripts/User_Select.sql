SELECT u.*, ch.[Timestamp] AS [DateAdded], mh.[Timestamp] AS [DateModified], s.[Enabled]
FROM [User] u
/**join**/
LEFT JOIN [History] ch ON ch.Id = u.Id AND ch.HistoryTypeId = 0
LEFT JOIN [History] mh ON mh.Id = u.Id AND ch.HistoryTypeId = 1
LEFT JOIN [Status] s ON s.Id = u.Id
/**where**/
GROUP BY u.Id
HAVING (MAX(mh.[Timestamp]) OR mh.[Timestamp] IS NULL) OR (MIN(ch.[Timestamp]) OR ch.[Timestamp] IS NULL)
/**orderby**/