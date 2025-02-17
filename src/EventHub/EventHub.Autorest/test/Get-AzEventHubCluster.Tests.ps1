if(($null -eq $TestName) -or ($TestName -contains 'Get-AzEventHubCluster'))
{
  $loadEnvPath = Join-Path $PSScriptRoot 'loadEnv.ps1'
  if (-Not (Test-Path -Path $loadEnvPath)) {
      $loadEnvPath = Join-Path $PSScriptRoot '..\loadEnv.ps1'
  }
  . ($loadEnvPath)
  $TestRecordingFile = Join-Path $PSScriptRoot 'Get-AzEventHubCluster.Recording.json'
  $currentPath = $PSScriptRoot
  while(-not $mockingPath) {
      $mockingPath = Get-ChildItem -Path $currentPath -Recurse -Include 'HttpPipelineMocking.ps1' -File
      $currentPath = Split-Path -Path $currentPath -Parent
  }
  . ($mockingPath | Select-Object -First 1).FullName
}

Describe 'Get-AzEventHubCluster' {
    It 'List' -skip {
        $listOfCluster = Get-AzEventHubCluster
        $listOfCluster.Count | Should -BeGreaterThan 10
    }

    It 'Get' -skip {
        $cluster = Get-AzEventHubCluster -ResourceGroupName $env.clusterResourceGroup -Name $env.createdCluster
        $cluster.Name | Should -Be $env.createdCluster
        $cluster.Capacity | Should -Be 1
        $cluster.SkuName | Should -Be "Dedicated"
    }

    It 'List1' -skip {
        $listOfCluster = Get-AzEventHubCluster -ResourceGroupName $env.clusterResourceGroup
        $listOfCluster.Count | Should -BeGreaterOrEqual 1
    }

    It 'GetViaIdentity' -skip {
        $cluster = Get-AzEventHubCluster -ResourceGroupName $env.clusterResourceGroup -Name $env.createdCluster
        
        $cluster = Get-AzEventHubCluster -InputObject $cluster
        $cluster.Name | Should -Be $env.createdCluster
        $cluster.Capacity | Should -Be 1
        $cluster.SkuName | Should -Be "Dedicated"

        $cluster = Get-AzEventHubCluster -InputObject $cluster.Id
        $cluster.Name | Should -Be $env.createdCluster
        $cluster.Capacity | Should -Be 1
        $cluster.SkuName | Should -Be "Dedicated"
    }
}
