pipeline {
    agent any

    triggers {
        pollSCM('* * * * *')
    }

    environment {
        SOLUTION_FILE = 'src\\MyWindowsService\\MyWindowsService.sln'
        MSBUILD_EXE   = "${tool 'MSBuild 4.0'}"
        DEPLOY_PATH   = 'C:\\Deploy\\MyWindowsService'
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
                bat "nuget.exe restore \"%SOLUTION_FILE%\""
            }
        }

        stage('Build') {
            steps {
                bat "\"%MSBUILD_EXE%\\MSBuild.exe\" \"%SOLUTION_FILE%\" /t:Build /p:Configuration=Release /nologo /verbosity:minimal"
            }
        }

        stage('Archive Artifact') {
            steps {
                archiveArtifacts artifacts: 'src/MyWindowsService/MyWindowsService/bin/Release/**', fingerprint: true
            }
        }

        stage('Deploy Local') {
            steps {
                bat '''
                    sc stop MyWindowsService
                    ping -n 8 127.0.0.1 >nul
                    sc delete MyWindowsService
                    ping -n 5 127.0.0.1 >nul
                    if not exist "%DEPLOY_PATH%" mkdir "%DEPLOY_PATH%"
                    xcopy /Y /E "src\\MyWindowsService\\MyWindowsService\\bin\\Release\\*" "%DEPLOY_PATH%\\"
                    sc create MyWindowsService binPath= "%DEPLOY_PATH%\\MyWindowsService.exe"
                    sc start MyWindowsService
                '''
            }
        }
    }
}