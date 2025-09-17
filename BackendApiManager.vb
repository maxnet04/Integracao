Imports System
Imports System.IO
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports System.Net.Http
Imports System.Windows.Forms

''' <summary>
''' Gerenciador do backend Node.js (suat-backend.exe)
''' Integrado ao sistema de orquestração
''' </summary>
Public Class BackendApiManager
    Private backendProcess As Process
    Private ReadOnly backendExePath As String
    Private ReadOnly backendPort As Integer = 3000
    Private httpClient As HttpClient

    ' Eventos para comunicação com a UI
    Public Event StatusChanged(message As String)
    Public Event ServerStarted(url As String)
    Public Event ServerStopped()
    Public Event HealthCheckResult(isHealthy As Boolean, message As String)

    Public Sub New(Optional backendExePath As String = Nothing)
        If String.IsNullOrEmpty(backendExePath) Then
            ' Caminho padrão do backend
            Me.backendExePath = Path.Combine(Application.StartupPath, "backend", "suat-backend.exe")
        Else
            Me.backendExePath = backendExePath
        End If
        
        ' Inicializar HttpClient com timeout
        httpClient = New HttpClient()
        httpClient.Timeout = TimeSpan.FromSeconds(5)
    End Sub

    Public ReadOnly Property IsRunning As Boolean
        Get
            Return backendProcess IsNot Nothing AndAlso Not backendProcess.HasExited
        End Get
    End Property

    Public ReadOnly Property ApiUrl As String
        Get
            Return $"http://localhost:{backendPort}"
        End Get
    End Property

    Public ReadOnly Property SwaggerUrl As String
        Get
            Return $"http://localhost:{backendPort}/api-docs"
        End Get
    End Property

    ''' <summary>
    ''' Inicia o backend Node.js
    ''' </summary>
    Public Function StartAsync() As Task(Of Boolean)
        Return Task.Run(Function() StartSync())
    End Function

    ''' <summary>
    ''' Inicia o backend sincronamente
    ''' </summary>
    Private Function StartSync() As Boolean
        Try
            If IsRunning Then
                RaiseEvent StatusChanged("Backend API já está rodando")
                Return True
            End If

            ' Verificar se executável existe
            If Not File.Exists(backendExePath) Then
                RaiseEvent StatusChanged($"❌ Backend não encontrado: {backendExePath}")
                Return False
            End If

            ' Verificar se porta está disponível
            If Not IsPortAvailable(backendPort) Then
                RaiseEvent StatusChanged($"⚠️ Porta {backendPort} já está em uso")
                ' Tentar parar processo existente
                StopExistingBackend()
                Threading.Thread.Sleep(2000)
            End If

            ' Configurar processo
            backendProcess = New Process()
            backendProcess.StartInfo.FileName = backendExePath
            backendProcess.StartInfo.UseShellExecute = False
            backendProcess.StartInfo.CreateNoWindow = True
            backendProcess.StartInfo.RedirectStandardOutput = True
            backendProcess.StartInfo.RedirectStandardError = True

            ' Eventos de monitoramento
            AddHandler backendProcess.OutputDataReceived, AddressOf OnBackendOutput
            AddHandler backendProcess.ErrorDataReceived, AddressOf OnBackendError
            AddHandler backendProcess.Exited, AddressOf OnBackendExited
            backendProcess.EnableRaisingEvents = True

            ' Iniciar processo
            RaiseEvent StatusChanged("🚀 Iniciando backend API...")
            backendProcess.Start()
            backendProcess.BeginOutputReadLine()
            backendProcess.BeginErrorReadLine()

            ' Aguardar inicialização e verificar saúde
            Return WaitForBackendStartup()

        Catch ex As Exception
            RaiseEvent StatusChanged($"❌ Erro ao iniciar backend: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Aguarda o backend inicializar e verifica se está funcionando
    ''' </summary>
    Private Function WaitForBackendStartup() As Boolean
        Try
            RaiseEvent StatusChanged("⏳ Aguardando backend inicializar...")

            ' Aguardar até 30 segundos
            For i As Integer = 1 To 30
                Threading.Thread.Sleep(1000)

                If Not IsRunning Then
                    RaiseEvent StatusChanged("❌ Backend falhou ao iniciar")
                    Return False
                End If

                ' Tentar fazer health check
                If PerformHealthCheck() Then
                    RaiseEvent StatusChanged("✅ Backend API iniciado com sucesso")
                    RaiseEvent ServerStarted(ApiUrl)
                    Return True
                End If

                RaiseEvent StatusChanged($"⏳ Aguardando backend... ({i}/30)")
            Next

            RaiseEvent StatusChanged("⏰ Timeout - Backend não respondeu em 30 segundos")
            Return False

        Catch ex As Exception
            RaiseEvent StatusChanged($"❌ Erro ao verificar backend: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Verifica se o backend está respondendo
    ''' </summary>
    Private Function PerformHealthCheck() As Boolean
        Try
            Dim response = httpClient.GetAsync($"{ApiUrl}/health").Result

            If response.IsSuccessStatusCode Then
                Return True
            End If

        Catch ex As Exception
            ' Ignorar erro - ainda não está pronto
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Para o backend API
    ''' </summary>
    Public Sub [Stop]()
        Try
            If IsRunning Then
                RaiseEvent StatusChanged("🛑 Parando backend API...")
                
                backendProcess.Kill()
                backendProcess.WaitForExit(5000)
                
                RaiseEvent StatusChanged("✅ Backend API parado")
            End If

        Catch ex As Exception
            RaiseEvent StatusChanged($"⚠️ Erro ao parar backend: {ex.Message}")
        Finally
            backendProcess = Nothing
            ' Reinicializar HttpClient para evitar problemas de timeout
            ResetHttpClient()
            RaiseEvent ServerStopped()
        End Try
    End Sub
    
    ''' <summary>
    ''' Reinicializa o HttpClient para evitar problemas de timeout
    ''' </summary>
    Private Sub ResetHttpClient()
        Try
            If httpClient IsNot Nothing Then
                httpClient.Dispose()
            End If
            httpClient = New HttpClient()
            httpClient.Timeout = TimeSpan.FromSeconds(5)
        Catch ex As Exception
            ' Em caso de erro, criar um novo HttpClient
            httpClient = New HttpClient()
            httpClient.Timeout = TimeSpan.FromSeconds(5)
        End Try
    End Sub

    ''' <summary>
    ''' Executa health check manual
    ''' </summary>
    Public Async Sub CheckHealthAsync()
        Try
            If Not IsRunning Then
                RaiseEvent HealthCheckResult(False, "Backend não está rodando")
                Return
            End If

            Dim response = Await httpClient.GetAsync($"{ApiUrl}/health")
            
            If response.IsSuccessStatusCode Then
                Dim content = Await response.Content.ReadAsStringAsync()
                RaiseEvent HealthCheckResult(True, $"Backend OK - {response.StatusCode}")
            Else
                RaiseEvent HealthCheckResult(False, $"Backend Error - {response.StatusCode}")
            End If

        Catch ex As Exception
            RaiseEvent HealthCheckResult(False, $"Health check falhou: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Verifica se uma porta está disponível
    ''' </summary>
    Private Function IsPortAvailable(port As Integer) As Boolean
        Try
            Dim listener = New Net.Sockets.TcpListener(Net.IPAddress.Any, port)
            listener.Start()
            listener.Stop()
            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Para processos existentes do backend
    ''' </summary>
    Private Sub StopExistingBackend()
        Try
            For Each proc In Process.GetProcessesByName("suat-backend")
                Try
                    proc.Kill()
                    proc.WaitForExit(2000)
                Catch
                    ' Ignorar erro
                End Try
            Next
        Catch ex As Exception
            RaiseEvent StatusChanged($"⚠️ Erro ao parar backend existente: {ex.Message}")
        End Try
    End Sub

    ' --- Eventos do Processo ---

    Private Sub OnBackendOutput(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            RaiseEvent StatusChanged($"[Backend] {e.Data}")
        End If
    End Sub

    Private Sub OnBackendError(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            RaiseEvent StatusChanged($"[Backend Error] {e.Data}")
        End If
    End Sub

    Private Sub OnBackendExited(sender As Object, e As EventArgs)
        RaiseEvent StatusChanged("⚠️ Backend API foi encerrado")
        RaiseEvent ServerStopped()
    End Sub

    ''' <summary>
    ''' Cleanup
    ''' </summary>
    Public Sub Dispose()
        [Stop]()
        httpClient?.Dispose()
    End Sub
End Class

