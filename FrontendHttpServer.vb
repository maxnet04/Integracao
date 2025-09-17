Imports System
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Windows.Forms

''' <summary>
''' Servidor HTTP integrado para servir o frontend React
''' Integrado ao SuatDatabaseManager
''' </summary>
Public Class FrontendHttpServer
    Private ReadOnly buildPath As String
    Private ReadOnly port As Integer
    Private listener As HttpListener
    Private cancellationTokenSource As CancellationTokenSource
    Private isRunning As Boolean = False

    ' Eventos para comunicação com a UI
    Public Event StatusChanged(message As String)
    Public Event ServerStarted(url As String)
    Public Event ServerStopped()
    Public Event RequestReceived(method As String, path As String)

    ' MIME Types
    Private ReadOnly mimeTypes As New Dictionary(Of String, String) From {
        {".html", "text/html; charset=utf-8"},
        {".css", "text/css; charset=utf-8"},
        {".js", "application/javascript; charset=utf-8"},
        {".json", "application/json; charset=utf-8"},
        {".png", "image/png"},
        {".jpg", "image/jpeg"},
        {".jpeg", "image/jpeg"},
        {".gif", "image/gif"},
        {".ico", "image/x-icon"},
        {".svg", "image/svg+xml"},
        {".woff", "font/woff"},
        {".woff2", "font/woff2"},
        {".ttf", "font/ttf"},
        {".eot", "application/vnd.ms-fontobject"},
        {".map", "application/json"},
        {".txt", "text/plain"}
    }

    Public Sub New(buildPath As String, Optional port As Integer = 8080)
        Me.buildPath = buildPath
        Me.port = port
    End Sub

    Public ReadOnly Property IsServerRunning As Boolean
        Get
            Return isRunning
        End Get
    End Property

    Public ReadOnly Property ServerUrl As String
        Get
            Return $"http://localhost:{port}"
        End Get
    End Property

    ''' <summary>
    ''' Inicia o servidor HTTP em background
    ''' </summary>
    Public Function StartAsync() As Boolean
        Try
            If isRunning Then
                RaiseEvent StatusChanged("Servidor já está rodando")
                Return True
            End If

            ' Verificar se build existe
            If Not Directory.Exists(buildPath) Then
                RaiseEvent StatusChanged($"❌ Pasta build não encontrada: {buildPath}")
                Return False
            End If

            ' Verificar se index.html existe
            Dim indexPath = Path.Combine(buildPath, "index.html")
            If Not File.Exists(indexPath) Then
                RaiseEvent StatusChanged($"❌ index.html não encontrado em: {buildPath}")
                Return False
            End If

            ' Inicializar HttpListener
            listener = New HttpListener()
            listener.Prefixes.Add($"http://localhost:{port}/")
            
            Try
                listener.Start()
            Catch ex As HttpListenerException
                If ex.ErrorCode = 5 Then
                    RaiseEvent StatusChanged($"❌ Acesso negado na porta {port}. Execute como Administrador")
                Else
                    RaiseEvent StatusChanged($"❌ Erro na porta {port}: {ex.Message}")
                End If
                Return False
            End Try

            cancellationTokenSource = New CancellationTokenSource()
            isRunning = True

            ' Iniciar processamento em background
            Task.Run(Sub() ProcessRequestsAsync(cancellationTokenSource.Token))

            Dim fileCount = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories).Length
            RaiseEvent StatusChanged($"✅ Servidor frontend iniciado")
            RaiseEvent StatusChanged($"📁 Servindo {fileCount} arquivos de: {buildPath}")
            RaiseEvent ServerStarted(ServerUrl)

            Return True

        Catch ex As Exception
            RaiseEvent StatusChanged($"❌ Erro ao iniciar servidor: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Para o servidor HTTP
    ''' </summary>
    Public Sub [Stop]()
        Try
            If Not isRunning Then
                Return
            End If

            RaiseEvent StatusChanged("🛑 Parando servidor frontend...")

            cancellationTokenSource?.Cancel()
            listener?.Stop()
            listener?.Close()
            isRunning = False

            RaiseEvent StatusChanged("✅ Servidor frontend parado")
            RaiseEvent ServerStopped()

        Catch ex As Exception
            RaiseEvent StatusChanged($"⚠️ Erro ao parar servidor: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Processa requests HTTP em background
    ''' </summary>
    Private Sub ProcessRequestsAsync(cancellationToken As CancellationToken)
        While Not cancellationToken.IsCancellationRequested AndAlso listener.IsListening
            Try
                Dim context = listener.GetContext()
                ' Processar request em background para não bloquear
                Task.Run(Sub() HandleRequest(context))

            Catch ex As ObjectDisposedException
                ' Listener foi fechado - normal
                Exit While
            Catch ex As Exception
                If Not cancellationToken.IsCancellationRequested Then
                    RaiseEvent StatusChanged($"⚠️ Erro ao processar request: {ex.Message}")
                End If
            End Try
        End While
    End Sub

    ''' <summary>
    ''' Manipula uma requisição HTTP
    ''' </summary>
    Private Sub HandleRequest(context As HttpListenerContext)
        Dim request = context.Request
        Dim response = context.Response
        Dim requestPath = request.Url.LocalPath

        Try
            ' Log da requisição
            RaiseEvent RequestReceived(request.HttpMethod, requestPath)

            ' Normalizar caminho
            If requestPath = "/" Then
                requestPath = "/index.html"
            End If

            ' Remover query string
            If requestPath.Contains("?") Then
                requestPath = requestPath.Substring(0, requestPath.IndexOf("?"))
            End If

            ' Caminho físico do arquivo
            Dim filePath = Path.Combine(buildPath, requestPath.TrimStart("/"c))

            ' Verificar se arquivo existe
            If File.Exists(filePath) Then
                ServeFile(response, filePath)
            Else
                ' SPA Fallback - servir index.html para rotas do React Router
                If Not requestPath.StartsWith("/static/") And Not requestPath.Contains(".") Then
                    Dim indexPath = Path.Combine(buildPath, "index.html")
                    If File.Exists(indexPath) Then
                        ServeFile(response, indexPath)
                    Else
                        SendError(response, 404, "index.html not found")
                    End If
                Else
                    SendError(response, 404, $"File not found: {requestPath}")
                End If
            End If

        Catch ex As Exception
            SendError(response, 500, "Internal Server Error")
        End Try
    End Sub

    ''' <summary>
    ''' Serve um arquivo
    ''' </summary>
    Private Sub ServeFile(response As HttpListenerResponse, filePath As String)
        Try
            Dim fileBytes = File.ReadAllBytes(filePath)
            Dim extension = Path.GetExtension(filePath).ToLower()

            ' Content-Type
            Dim contentType = "application/octet-stream"
            If mimeTypes.ContainsKey(extension) Then
                contentType = mimeTypes(extension)
            End If

            ' Headers
            response.ContentType = contentType
            response.ContentLength64 = fileBytes.Length
            response.StatusCode = 200

            ' Cache headers
            If extension <> ".html" Then
                response.Headers.Add("Cache-Control", "public, max-age=31536000")
            Else
                response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate")
            End If

            ' CORS headers (se necessário)
            response.Headers.Add("Access-Control-Allow-Origin", "*")

            ' Enviar arquivo
            response.OutputStream.Write(fileBytes, 0, fileBytes.Length)
            response.Close()

        Catch ex As Exception
            SendError(response, 500, "Error serving file")
        End Try
    End Sub

    ''' <summary>
    ''' Envia resposta de erro
    ''' </summary>
    Private Sub SendError(response As HttpListenerResponse, statusCode As Integer, message As String)
        Try
            response.StatusCode = statusCode
            response.ContentType = "text/plain; charset=utf-8"

            Dim responseBytes = Encoding.UTF8.GetBytes(message)
            response.ContentLength64 = responseBytes.Length
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length)
            response.Close()

        Catch ex As Exception
            Try
                response.Close()
            Catch
                ' Ignorar erro ao fechar
            End Try
        End Try
    End Sub
End Class
