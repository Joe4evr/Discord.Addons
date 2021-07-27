foreach ($analyzerFilePath in Get-ChildItem $analyzersPath -Filter *.dll)
{
    if ($project.Object.AnalyzerReferences)
    {
        $project.Object.AnalyzerReferences.Remove($analyzerFilePath.FullName)
    }
}
