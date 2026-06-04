<#
.SYNOPSIS
    Exporta documentación de iteración de Markdown a Word (.docx) usando Pandoc.

.DESCRIPTION
    Usa el .docx de la plantilla como reference-doc para preservar estilos (fuentes,
    headings, tablas) y convierte el markdown de la iteración al formato requerido
    para la entrega en el drive de Teams.

.PARAMETER Iteracion
    Número de iteración a exportar (ej: 3, 4, 5)

.PARAMETER OutputDir
    Directorio de salida. Por defecto: la carpeta de la iteración en el drive de Teams.

.EXAMPLE
    .\export-doc.ps1 -Iteracion 3
    .\export-doc.ps1 -Iteracion 3 -OutputDir "C:\Users\sebas\Desktop"
#>
param(
    [Parameter(Mandatory = $true)]
    [int]$Iteracion,

    [Parameter(Mandatory = $false)]
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

# --- Rutas ---
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$seguimientoDir = Join-Path $repoRoot "docs\seguimiento"
$inputFile = Join-Path $seguimientoDir "iteracion-$Iteracion.md"
$referenceDoc = Join-Path $seguimientoDir "Plantilla_doumentacion_iteraciones.docx"
$pandoc = "C:\Users\sebas\AppData\Local\Pandoc\pandoc.exe"

# Directorio de salida por defecto: drive de Teams
if (-not $OutputDir) {
    $OutputDir = $seguimientoDir
}

$outputFile = Join-Path $OutputDir "Documentacion_It.$Iteracion.docx"

# --- Validaciones ---
if (-not (Test-Path $pandoc)) {
    Write-Error "Pandoc no encontrado en: $pandoc"
    exit 1
}

if (-not (Test-Path $inputFile)) {
    Write-Error "Archivo de entrada no encontrado: $inputFile`nCrealo a partir del template: docs\seguimiento\template-iteracion.md"
    exit 1
}

if (-not (Test-Path $referenceDoc)) {
    Write-Warning "Reference doc no encontrado: $referenceDoc`nSe generará sin estilos de plantilla."
    $useReference = $false
} else {
    $useReference = $true
}

# Crear directorio de salida si no existe
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Creado directorio: $OutputDir"
}

# --- Exportación ---
$pandocArgs = @(
    $inputFile
    "-o", $outputFile
    "--from", "markdown"
    "--to", "docx"
    "--resource-path", $seguimientoDir
)

if ($useReference) {
    $pandocArgs += "--reference-doc", $referenceDoc
}

Write-Host "Exportando iteracion $Iteracion..."
Write-Host "  Input:  $inputFile"
Write-Host "  Output: $outputFile"
if ($useReference) {
    Write-Host "  Estilos: plantilla reference-doc"
}

& $pandoc @pandocArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nExportacion exitosa: $outputFile" -ForegroundColor Green
} else {
    Write-Error "Pandoc fallo con codigo de salida: $LASTEXITCODE"
    exit 1
}
