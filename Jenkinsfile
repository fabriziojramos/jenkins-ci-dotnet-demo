// Jenkinsfile para MyWindowsService (.NET Framework 4.5.2)
// Agente: Windows local (mismo host que Jenkins), MSBuild via "Build Tools 2022"
// configurado en Manage Jenkins > Global Tool Configuration > MSBuild installations.

pipeline {
    agent any

    environment {
        SOLUTION_FILE = 'src\\MyWindowsService\\MyWindowsService.sln'
        MSBUILD_EXE   = "${tool 'MSBuild 4.0'}"
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'master',
                    url: 'https://github.com/fabriziojramos/jenkins-ci-dotnet-demo.git'
            }
        }

        stage('Restore') {
            steps {
                // nuget.exe ya viene incluido en la raiz del repo
                bat "nuget.exe restore \"%SOLUTION_FILE%\""
            }
        }

        stage('Build') {
            steps {
                bat "\"%MSBUILD_EXE%\" \"%SOLUTION_FILE%\" /t:Build /p:Configuration=Release /nologo /verbosity:minimal"
            }
        }

        stage('Archive Artifact') {
            steps {
                archiveArtifacts artifacts: 'src/MyWindowsService/MyWindowsService/bin/Release/**', fingerprint: true
            }
        }
    }
}