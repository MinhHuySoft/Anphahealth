#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$Server = 'localhost'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$mediaDirectory = 'src/AnphaHealth.Web/wwwroot'
$sqlcmd = Get-Command sqlcmd -CommandType Application | Select-Object -First 1

function Read-RepositoryJson([string]$RelativePath) {
    return Get-Content -LiteralPath (Join-Path $repositoryRoot $RelativePath) -Raw -Encoding utf8 |
        ConvertFrom-Json -AsHashtable
}

function Resolve-RepositoryFile([string]$RelativePath) {
    $fullPath = [IO.Path]::GetFullPath([IO.Path]::Combine($repositoryRoot, $RelativePath))
    $rootPrefix = [IO.Path]::TrimEndingDirectorySeparator($repositoryRoot) + [IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        !(Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing or invalid repository file: $RelativePath"
    }
    return $fullPath
}

function Find-MediaPath([string]$FileName) {
    if (!$assetPathsByFileName.ContainsKey($FileName)) {
        throw "Cannot resolve a CMS seed image: $FileName"
    }
    return $assetPathsByFileName[$FileName]
}

$manifest = @(Read-RepositoryJson 'data/asset-manifest.json')
$pages = @(Read-RepositoryJson 'data/anpha-content.json')
$catalog = Read-RepositoryJson 'database/seed-catalog.json'
$assetPathsByFileName = @{}
$assetContentTypes = @{}

foreach ($asset in $manifest) {
    if ($asset.local -notmatch '^/media/[0-9A-Fa-f]+-(?<FileName>[^/\\]+)$') {
        throw "Unexpected media path: $($asset.local)"
    }
    $asset.filename = $Matches.FileName
    $assetRelativePath = $mediaDirectory + $asset.local
    $absolutePath = Resolve-RepositoryFile $assetRelativePath
    $actualLength = (Get-Item -LiteralPath $absolutePath).Length
    if ($actualLength -ne $asset.bytes) {
        throw "Media size differs from the crawl manifest: $assetRelativePath"
    }
    $assetPathsByFileName[$asset.filename] = $asset.local
    $assetContentTypes[$assetRelativePath] = $asset.type
}

$rawDirectory = Join-Path $repositoryRoot 'RECON/raw'
$rawFiles = @()
if (Test-Path -LiteralPath $rawDirectory -PathType Container) {
    $rawFiles = @(Get-ChildItem -LiteralPath $rawDirectory -File |
        Where-Object { $_.Name -notlike 'chemlabs-*' })
}
else {
    Write-Warning 'RECON/raw is absent. Importing normalized content and available media.'
}
$wordpressRecords = @{}
foreach ($rawFile in $rawFiles) {
    if ($rawFile.Name -match '^wp-json_wp_v2_(pages|posts|media)-\d+\.json$') {
        foreach ($record in @(Read-RepositoryJson ('RECON/raw/' + $rawFile.Name))) {
            $wordpressRecords[$record.link.TrimEnd('/')] = $record
        }
    }
}

$productBySlug = @{}
foreach ($product in $catalog.products) {
    $product.imagePath = Find-MediaPath $product.image
    $productBySlug.Add($product.slug, $product)
}

$articleBySlug = @{}
foreach ($article in $catalog.articles) {
    $article.imagePath = Find-MediaPath $article.image
    $articleBySlug.Add($article.slug, $article)
}

$pageSlugs = @{}
foreach ($page in $pages) {
    $pageSlugs.Add($page.slug, $true)
    foreach ($imagePath in $page.localImages) {
        if (!$assetContentTypes.ContainsKey($mediaDirectory + $imagePath)) {
            throw "Missing media $imagePath for crawled page $($page.slug)"
        }
    }

    $wordpress = $wordpressRecords[$page.url.TrimEnd('/')]
    $page.publishedAt = $null
    if ($null -ne $wordpress) {
        $page.publishedAt = $wordpress.date_gmt
    }
    $page.sourceMetadata = [ordered]@{
        headings = @($page.headings)
        links = @($page.links)
        images = @($page.images)
        wordpress = $wordpress
    }
    $page.imagePath = $null
    $page.summary = ''
    $page.kind = 'Archive'
    if ($productBySlug.ContainsKey($page.slug)) {
        $product = $productBySlug[$page.slug]
        $page.kind = 'Product'
        $page.summary = $product.description
        $page.imagePath = $product.imagePath
    }
    elseif ($articleBySlug.ContainsKey($page.slug)) {
        $article = $articleBySlug[$page.slug]
        $page.kind = 'Article'
        $page.summary = $article.summary
        if (!$page.publishedAt) { $page.publishedAt = $article.publishedAt }
        $page.imagePath = $article.imagePath
    }
    elseif ($page.slug -in $catalog.pages) {
        $page.kind = 'Page'
    }
}

foreach ($seed in @($catalog.products) + @($catalog.articles)) {
    if (!$pageSlugs.ContainsKey($seed.slug)) {
        throw "Missing crawled page for CMS seed: $($seed.slug)"
    }
}

& $sqlcmd.Source -S $Server -E -C -b -i (Join-Path $PSScriptRoot 'schema.sql')
if ($LASTEXITCODE -ne 0) {
    throw "Could not apply SQL Server schema. sqlcmd exited with code $LASTEXITCODE."
}

$connectionOptions = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$connectionOptions['Data Source'] = $Server
$connectionOptions['Initial Catalog'] = 'AnphaHealth'
$connectionOptions['Integrated Security'] = $true
$connectionOptions['Encrypt'] = $true
$connectionOptions['TrustServerCertificate'] = $true
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionOptions.ConnectionString)

try {
    $connection.Open()
    $transaction = $connection.BeginTransaction()
    try {
        $seedCommand = $connection.CreateCommand()
        $seedCommand.Transaction = $transaction
        $seedCommand.CommandTimeout = 120
        $seedCommand.CommandText = @'
DECLARE @LockResult int;
EXEC @LockResult = sys.sp_getapplock
    @Resource = N'AnphaHealth.CrawlImport', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
IF @LockResult < 0
    THROW 50001, 'Cannot acquire the crawl import lock.', 1;
'@ + [Environment]::NewLine + (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'seed.sql') -Raw -Encoding utf8)

        $seedCommand.Parameters.Add('@MediaJson', [System.Data.SqlDbType]::NVarChar, -1).Value =
            ConvertTo-Json -InputObject $manifest -Depth 32 -Compress
        $seedCommand.Parameters.Add('@PagesJson', [System.Data.SqlDbType]::NVarChar, -1).Value =
            ConvertTo-Json -InputObject $pages -Depth 64 -Compress
        $seedCommand.Parameters.Add('@CatalogJson', [System.Data.SqlDbType]::NVarChar, -1).Value =
            ConvertTo-Json -InputObject $catalog -Depth 16 -Compress
        [void]$seedCommand.ExecuteNonQuery()
        $seedCommand.Dispose()

        $snapshotCommand = $connection.CreateCommand()
        $snapshotCommand.Transaction = $transaction
        $snapshotCommand.CommandText = @'
INSERT dbo.SourceSnapshots (RelativePath, ContentType, Payload, Sha256)
SELECT @Path, @Type, @Payload, @Sha256
WHERE NOT EXISTS (SELECT 1 FROM dbo.SourceSnapshots WHERE RelativePath = @Path);
'@
        $pathParameter = $snapshotCommand.Parameters.Add('@Path', [System.Data.SqlDbType]::NVarChar, 450)
        $typeParameter = $snapshotCommand.Parameters.Add('@Type', [System.Data.SqlDbType]::NVarChar, 150)
        $payloadParameter = $snapshotCommand.Parameters.Add('@Payload', [System.Data.SqlDbType]::VarBinary, -1)
        $hashParameter = $snapshotCommand.Parameters.Add('@Sha256', [System.Data.SqlDbType]::Char, 64)

        $rawPaths = @($rawFiles | ForEach-Object { 'RECON/raw/' + $_.Name })
        $snapshotPaths = $rawPaths + @($assetContentTypes.Keys) + @(
            'data/anpha-content.json', 'data/asset-manifest.json',
            'RECON/routes/anpha-route-map.json', 'RECON/coverage-audit.json'
        )
        foreach ($relativePath in $snapshotPaths) {
            if ($relativePath.StartsWith('RECON/', [StringComparison]::Ordinal) -and
                !(Test-Path -LiteralPath (Join-Path $repositoryRoot $relativePath) -PathType Leaf)) {
                continue
            }
            $absolutePath = Resolve-RepositoryFile $relativePath
            $payload = [IO.File]::ReadAllBytes($absolutePath)
            $contentType = switch ([IO.Path]::GetExtension($relativePath)) {
                '.html' { 'text/html; charset=utf-8' }
                '.json' { 'application/json' }
                '.txt'  { 'text/plain; charset=utf-8' }
                '.xml'  { 'application/xml' }
                default { 'application/octet-stream' }
            }
            if ($assetContentTypes.ContainsKey($relativePath)) {
                $contentType = $assetContentTypes[$relativePath]
            }
            $pathParameter.Value = $relativePath
            $typeParameter.Value = $contentType
            $payloadParameter.Value = $payload
            $hashParameter.Value = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash
            [void]$snapshotCommand.ExecuteNonQuery()
        }
        $snapshotCommand.Dispose()
        $transaction.Commit()
    }
    catch {
        if ($null -ne $transaction.Connection) {
            $transaction.Rollback()
        }
        throw
    }
    finally {
        $transaction.Dispose()
    }

    $countCommand = $connection.CreateCommand()
    $countCommand.CommandText = @'
SELECT (SELECT COUNT(*) FROM dbo.ContentPages) AS ContentPages,
       (SELECT COUNT(*) FROM dbo.MediaAssets) AS MediaAssets,
       (SELECT COUNT(*) FROM dbo.ContentPageMedia) AS PageMediaLinks,
       (SELECT COUNT(*) FROM dbo.ProductCategories) AS Categories,
       (SELECT COUNT(*) FROM dbo.Products) AS Products,
       (SELECT COUNT(*) FROM dbo.SiteSettings) AS Settings,
       (SELECT COUNT(*) FROM dbo.SourceSnapshots) AS SourceSnapshots;
'@
    $reader = $countCommand.ExecuteReader()
    try {
        [void]$reader.Read()
        $counts = [ordered]@{ Server = $Server; Database = 'AnphaHealth' }
        for ($index = 0; $index -lt $reader.FieldCount; $index++) {
            $counts[$reader.GetName($index)] = $reader.GetValue($index)
        }
        [PSCustomObject]$counts
    }
    finally {
        $reader.Dispose()
        $countCommand.Dispose()
    }
}
finally {
    $connection.Dispose()
}
