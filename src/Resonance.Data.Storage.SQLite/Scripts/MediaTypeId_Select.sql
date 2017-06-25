SELECT
DISTINCT
CASE
	WHEN t.Id IS NOT NULL THEN mtt.Id
	WHEN al.Id IS NOT NULL THEN mtal.Id
	WHEN ar.Id IS NOT NULL THEN mtar.Id
	ELSE NULL
END
FROM 
[MediaType] mtar,
[MediaType] mtal,
[MediaType] mtt
LEFT JOIN [Track] t ON t.Id = @Id
LEFT JOIN [Album] al ON al.Id = @Id
LEFT JOIN [Artist] ar ON ar.Id = @Id
WHERE mtar.Id = 0
AND mtal.Id = 1
AND mtt.Id = 2