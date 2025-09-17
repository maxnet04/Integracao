Imports System
Imports System.IO
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Windows.Forms

''' <summary>
''' Gerenciador de portas do sistema SUAT-IA
''' Responsável por verificar, liberar e gerenciar portas utilizadas pelos serviços
''' Compatível com .NET Framework 4.7
''' </summary>
Public Class PortManager
    ' --- Constantes de Configuração ---
    Private Const BACKEND_PORT As Integer = 3000
    Private Const FRONTEND_PORT As Integer = 8080
    
    ' --- Eventos ---
    Public Event PortStatusChanged(port As Integer, status As String)
    Public Event ProcessKilled(processName As String, pid As Integer, port As Integer)
    Public Event PortFreed(port As Integer)
    Public Event PortCheckCompleted(success As Boolean, message As String)
    
    ''' <summary>
    ''' Verifica se uma porta está disponível (usando netstat para maior precisão)
    ''' </summary>
    Public Function IsPortAvailable(port As Integer) As Boolean
        Try
            ' Método 1: Usar netstat para verificar se a porta está em uso
            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = "netstat"
            startInfo.Arguments = $"-an"
            startInfo.UseShellExecute = False
            startInfo.RedirectStandardOutput = True
            startInfo.CreateNoWindow = True

            Using process As Process = Process.Start(startInfo)
                Dim output As String = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                ' Procurar por linhas que contêm a porta em estado LISTENING
                Dim lines = output.Split(Environment.NewLine)
                For Each line In lines
                    If (line.Contains($":{port} ") OrElse line.Contains($":{port}	")) AndAlso 
                       (line.Contains("LISTENING") OrElse line.Contains("ESTABLISHED")) Then
                        Return False ' Porta ocupada
                    End If
                Next
            End Using

            ' Método 2: Fallback com TcpListener (teste duplo para localhost e any)
            Try
                ' Testar localhost
                Dim listenerLocal = New Net.Sockets.TcpListener(Net.IPAddress.Loopback, port)
                listenerLocal.Start()
                listenerLocal.Stop()
                
                ' Testar any
                Dim listenerAny = New Net.Sockets.TcpListener(Net.IPAddress.Any, port)
                listenerAny.Start()
                listenerAny.Stop()
                
                Return True ' Ambos os testes passaram, porta disponível
            Catch
                Return False ' Algum teste falhou, porta ocupada
            End Try

        Catch ex As Exception
            ' Em caso de erro, assumir que a porta está ocupada por segurança
            Console.WriteLine($"⚠️ Erro ao verificar porta {port}: {ex.Message}")
            Return False
        End Try
    End Function
    
    ''' <summary>
    ''' Verifica e libera uma porta específica
    ''' </summary>
    Public Sub FreePort(port As Integer)
        Try
            RaiseEvent PortStatusChanged(port, $"🔍 Verificando porta {port}...")
            
            ' Verificar se porta está em uso
            If Not IsPortAvailable(port) Then
                RaiseEvent PortStatusChanged(port, $"⚠️ Porta {port} está em uso. Matando processos...")
                
                ' Encontrar e matar processos que usam a porta
                KillProcessesUsingPort(port)
                
                ' Aguardar um pouco para os processos terminarem
                Threading.Thread.Sleep(2000)
                
                ' Verificar novamente
                If Not IsPortAvailable(port) Then
                    ' Fallback: tentar matar processos Node.js do backend de forma assertiva
                    RaiseEvent PortStatusChanged(port, $"⚠️ Porta {port} ainda ocupada. Tentando matar processos Node.js do backend...")
                    KillBackendNodeProcesses()

                    ' Aguardar mais um pouco
                    Threading.Thread.Sleep(3000)

                    ' Verificação final
                    If Not IsPortAvailable(port) Then
                        RaiseEvent PortStatusChanged(port, $"❌ Não foi possível liberar a porta {port}")
                        RaiseEvent PortCheckCompleted(False, $"Porta {port} não pôde ser liberada")
                    Else
                        RaiseEvent PortStatusChanged(port, $"✅ Porta {port} liberada após encerrar processos Node.js")
                        RaiseEvent PortFreed(port)
                        RaiseEvent PortCheckCompleted(True, $"Porta {port} liberada após encerrar Node.js")
                    End If
                Else
                    RaiseEvent PortStatusChanged(port, $"✅ Porta {port} liberada com sucesso")
                    RaiseEvent PortFreed(port)
                    RaiseEvent PortCheckCompleted(True, $"Porta {port} liberada com sucesso")
                End If
            Else
                RaiseEvent PortStatusChanged(port, $"✅ Porta {port} já está disponível")
                RaiseEvent PortCheckCompleted(True, $"Porta {port} já estava disponível")
            End If

        Catch ex As Exception
            RaiseEvent PortStatusChanged(port, $"⚠️ Erro ao verificar porta {port}: {ex.Message}")
            RaiseEvent PortCheckCompleted(False, $"Erro ao verificar porta {port}: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Mata todos os processos que usam uma porta específica
    ''' </summary>
    Private Sub KillProcessesUsingPort(port As Integer)
        Try
            ' Usar netstat para encontrar processos que usam a porta
            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = "netstat"
            startInfo.Arguments = $"-ano"
            startInfo.UseShellExecute = False
            startInfo.RedirectStandardOutput = True
            startInfo.CreateNoWindow = True

            Dim process As Process = Process.Start(startInfo)
            Dim output As String = process.StandardOutput.ReadToEnd()
            process.WaitForExit()

            ' Procurar por linhas que contêm a porta
            Dim lines = output.Split(Environment.NewLine)
            Dim pidsToKill As New List(Of Integer)

            For Each line In lines
                If line.Contains($":{port} ") AndAlso line.Contains("LISTENING") Then
                    ' Extrair PID da linha
                    Dim parts = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
                    If parts.Length >= 5 Then
                        Dim pidStr = parts(parts.Length - 1)
                        Dim pid As Integer
                        If Integer.TryParse(pidStr, pid) Then
                            pidsToKill.Add(pid)
                        End If
                    End If
                End If
            Next

            ' Matar os processos encontrados
            For Each pid As Integer In pidsToKill
                Try
                    Dim proc = Process.GetProcessById(pid)
                    RaiseEvent PortStatusChanged(port, $"🔄 Matando processo {proc.ProcessName} (PID: {pid}) que usa porta {port}")
                    proc.Kill()
                    proc.WaitForExit(3000)
                    RaiseEvent ProcessKilled(proc.ProcessName, pid, port)
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(port, $"⚠️ Erro ao matar processo PID {pid}: {ex.Message}")
                End Try
            Next

        Catch ex As Exception
            RaiseEvent PortStatusChanged(port, $"❌ Erro ao verificar processos da porta {port}: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Fallback assertivo: encerra apenas processos Node.js relacionados ao backend (por linha de comando)
    ''' </summary>
    Private Sub KillBackendNodeProcesses()
        Try
            RaiseEvent PortStatusChanged(0, "🔄 Verificando processos Node.js do backend...")
            Dim nodeProcesses = Process.GetProcessesByName("node")
            Dim killedCount As Integer = 0
            For Each proc In nodeProcesses
                Try
                    Dim isBackend As Boolean = False
                    Dim processInfo As String = String.Empty
                    Try
                        processInfo = GetProcessCommandLine(proc.Id)
                        If processInfo.Contains("suat-backend") _
                           OrElse processInfo.ToLower().Contains("backend") _
                           OrElse processInfo.Contains("3000") _
                           OrElse processInfo.ToLower().Contains("server.js") _
                           OrElse processInfo.ToLower().Contains("app.js") Then
                            isBackend = True
                            RaiseEvent PortStatusChanged(0, $"🎯 Processo backend Node.js detectado: PID {proc.Id}")
                        End If
                    Catch
                        ' Se não conseguir obter a linha de comando, ser conservador e só encerrar se porta 3000 estiver ocupada
                        If Not IsPortAvailable(BACKEND_PORT) Then
                            isBackend = True
                            RaiseEvent PortStatusChanged(0, $"🤔 Processo Node.js sem info (PID: {proc.Id}) - encerrando por precaução")
                        End If
                    End Try

                    If isBackend Then
                        proc.Kill()
                        proc.WaitForExit(5000)
                        killedCount += 1
                        RaiseEvent ProcessKilled("node", proc.Id, 0)
                    End If
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(0, $"⚠️ Erro ao encerrar Node.js PID {proc.Id}: {ex.Message}")
                End Try
            Next

            If killedCount > 0 Then
                RaiseEvent PortStatusChanged(0, $"✅ {killedCount} processo(s) Node.js do backend encerrado(s)")
            Else
                RaiseEvent PortStatusChanged(0, "ℹ️ Nenhum processo Node.js relevante encontrado")
            End If

        Catch ex As Exception
            RaiseEvent PortStatusChanged(0, $"❌ Erro ao analisar processos Node.js: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Obtém a linha de comando de um processo (Windows via WMIC)
    ''' </summary>
    Private Function GetProcessCommandLine(processId As Integer) As String
        Try
            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = "wmic"
            startInfo.Arguments = $"process where processid={processId} get commandline /format:value"
            startInfo.UseShellExecute = False
            startInfo.RedirectStandardOutput = True
            startInfo.CreateNoWindow = True

            Using p As Process = Process.Start(startInfo)
                Dim output As String = p.StandardOutput.ReadToEnd()
                p.WaitForExit()
                Dim lines = output.Split(Environment.NewLine)
                For Each line In lines
                    If line.StartsWith("CommandLine=") Then
                        Return line.Substring("CommandLine=".Length)
                    End If
                Next
            End Using
        Catch
        End Try
        Return String.Empty
    End Function

    ''' <summary>
    ''' APENAS VERIFICA as portas do sistema SUAT-IA (SEM matar processos)
    ''' </summary>
    Public Sub CheckAllSystemPorts()
        Try
            RaiseEvent PortStatusChanged(0, "🔍 Iniciando verificação de portas do sistema...")
            
            ' Verificar porta do backend
            Dim backendAvailable = IsPortAvailable(BACKEND_PORT)
            If backendAvailable Then
                RaiseEvent PortStatusChanged(BACKEND_PORT, $"✅ Porta {BACKEND_PORT} (Backend) está LIVRE")
            Else
                RaiseEvent PortStatusChanged(BACKEND_PORT, $"⚠️ Porta {BACKEND_PORT} (Backend) está OCUPADA")
            End If
            
            ' Verificar porta do frontend
            Dim frontendAvailable = IsPortAvailable(FRONTEND_PORT)
            If frontendAvailable Then
                RaiseEvent PortStatusChanged(FRONTEND_PORT, $"✅ Porta {FRONTEND_PORT} (Frontend) está LIVRE")
            Else
                RaiseEvent PortStatusChanged(FRONTEND_PORT, $"⚠️ Porta {FRONTEND_PORT} (Frontend) está OCUPADA")
            End If
            
            RaiseEvent PortStatusChanged(0, "✅ Verificação de portas concluída")
            RaiseEvent PortCheckCompleted(True, $"Backend: {If(backendAvailable, "LIVRE", "OCUPADA")} | Frontend: {If(frontendAvailable, "LIVRE", "OCUPADA")}")
            
        Catch ex As Exception
            RaiseEvent PortStatusChanged(0, $"❌ Erro na verificação de portas: {ex.Message}")
            RaiseEvent PortCheckCompleted(False, $"Erro na verificação: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Verifica e libera todas as portas do sistema SUAT-IA (MATA processos se necessário)
    ''' </summary>
    Public Sub FreeAllSystemPorts()
        Try
            RaiseEvent PortStatusChanged(0, "🔍 Iniciando liberação de portas do sistema...")
            
            ' Liberar porta do backend
            FreePort(BACKEND_PORT)
            
            ' Liberar porta do frontend
            FreePort(FRONTEND_PORT)
            
            RaiseEvent PortStatusChanged(0, "✅ Liberação de portas concluída")
            
        Catch ex As Exception
            RaiseEvent PortStatusChanged(0, $"❌ Erro na liberação de portas: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' MATA TODOS os processos backend e para o frontend de forma controlada
    ''' </summary>
    Public Sub KillAllSystemProcesses(Optional frontendServer As Object = Nothing)
        Try
            RaiseEvent PortStatusChanged(0, "🔄 Iniciando encerramento de processos do sistema...")
            
            Dim killedBackend As Integer = 0
            Dim killedFrontend As Integer = 0
            
            ' === 1. MATAR PROCESSOS POR NOME ===
            
            ' Matar processos suat-backend por nome
            For Each p In Process.GetProcessesByName("suat-backend")
                Try
                    RaiseEvent PortStatusChanged(0, $"🔄 Encerrando suat-backend por nome (PID {p.Id})")
                    p.Kill()
                    p.WaitForExit(3000)
                    killedBackend += 1
                    RaiseEvent ProcessKilled("suat-backend", p.Id, BACKEND_PORT)
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(0, $"⚠️ Erro ao encerrar suat-backend PID {p.Id}: {ex.Message}")
                End Try
            Next

            ' === 2. PARAR FRONTEND DE FORMA CONTROLADA ===
            
            If frontendServer IsNot Nothing Then
                Try
                    RaiseEvent PortStatusChanged(0, "🔄 Parando frontend server de forma controlada...")
                    ' Usar reflexão para chamar o método Stop
                    Dim stopMethod = frontendServer.GetType().GetMethod("Stop")
                    If stopMethod IsNot Nothing Then
                        stopMethod.Invoke(frontendServer, Nothing)
                        killedFrontend += 1
                        RaiseEvent PortStatusChanged(0, "✅ Frontend server parado com sucesso")
                        RaiseEvent ProcessKilled("frontend-server", 0, FRONTEND_PORT)
                    Else
                        RaiseEvent PortStatusChanged(0, "⚠️ Método Stop não encontrado no frontend server")
                    End If
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(0, $"⚠️ Erro ao parar frontend server: {ex.Message}")
                End Try
            Else
                RaiseEvent PortStatusChanged(0, "ℹ️ Frontend server não fornecido - pulando parada controlada")
            End If
            
            ' === 3. MATAR PROCESSOS NODE.JS BACKEND POR LINHA DE COMANDO ===
            
            ' Matar APENAS processos Node.js relacionados ao backend
            For Each p In Process.GetProcessesByName("node")
                Try
                    Dim cmd = GetProcessCommandLine(p.Id)
                    Dim isBackend = cmd.ToLower().Contains("suat-backend") OrElse cmd.ToLower().Contains("backend") OrElse
                                   cmd.Contains("3000") OrElse cmd.ToLower().Contains("server.js") OrElse cmd.ToLower().Contains("app.js")
                    
                    If isBackend Then
                        RaiseEvent PortStatusChanged(0, $"🔄 Encerrando Node.js backend (PID {p.Id}) - cmd: {cmd.Substring(0, Math.Min(50, cmd.Length))}...")
                        p.Kill()
                        p.WaitForExit(3000)
                        killedBackend += 1
                        RaiseEvent ProcessKilled("node-backend", p.Id, BACKEND_PORT)
                    End If
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(0, $"⚠️ Erro ao verificar/encerrar Node.js PID {p.Id}: {ex.Message}")
                End Try
            Next

            ' === 4. MATAR PROCESSOS QUE OCUPAM A PORTA 3000 (BACKEND APENAS) ===
            
            RaiseEvent PortStatusChanged(0, "🔍 Verificando processos que ocupam a porta 3000 (backend)...")
            
            ' Matar processos que ocupam a porta 3000 (backend)
            Dim backendPortProcesses = GetProcessesUsingPort(BACKEND_PORT)
            For Each pid In backendPortProcesses
                Try
                    Dim proc = Process.GetProcessById(pid)
                    RaiseEvent PortStatusChanged(0, $"🔄 Encerrando processo na porta {BACKEND_PORT}: {proc.ProcessName} (PID {pid})")
                    proc.Kill()
                    proc.WaitForExit(3000)
                    killedBackend += 1
                    RaiseEvent ProcessKilled($"{proc.ProcessName}-port{BACKEND_PORT}", pid, BACKEND_PORT)
                Catch ex As Exception
                    RaiseEvent PortStatusChanged(0, $"⚠️ Erro ao encerrar processo PID {pid} na porta {BACKEND_PORT}: {ex.Message}")
                End Try
            Next
            
            ' Não mata processos na porta 8080 - frontend é parado de forma controlada

            RaiseEvent PortStatusChanged(0, $"✅ Encerramento concluído: {killedBackend} processos backend, {killedFrontend} processos frontend")
            
        Catch ex As Exception
            RaiseEvent PortStatusChanged(0, $"❌ Erro no encerramento de processos: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Obtém lista de PIDs de processos que estão usando uma porta específica
    ''' </summary>
    Private Function GetProcessesUsingPort(port As Integer) As List(Of Integer)
        Dim pids As New List(Of Integer)
        Try
            ' Usar netstat para encontrar processos que usam a porta
            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = "netstat"
            startInfo.Arguments = $"-ano"
            startInfo.UseShellExecute = False
            startInfo.RedirectStandardOutput = True
            startInfo.CreateNoWindow = True

            Using process As Process = Process.Start(startInfo)
                Dim output As String = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                ' Procurar por linhas que contêm a porta
                Dim lines = output.Split(Environment.NewLine)
                For Each line In lines
                    If (line.Contains($":{port} ") OrElse line.Contains($":{port}	")) AndAlso 
                       (line.Contains("LISTENING") OrElse line.Contains("ESTABLISHED")) Then
                        ' Extrair PID da linha
                        Dim parts = line.Split(New Char() {" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
                        If parts.Length >= 5 Then
                            Dim pidStr = parts(parts.Length - 1)
                            Dim pid As Integer
                            If Integer.TryParse(pidStr, pid) AndAlso Not pids.Contains(pid) Then
                                pids.Add(pid)
                            End If
                        End If
                    End If
                Next
            End Using

        Catch ex As Exception
            RaiseEvent PortStatusChanged(0, $"❌ Erro ao verificar processos da porta {port}: {ex.Message}")
        End Try
        
        Return pids
    End Function
    
    ''' <summary>
    ''' Verifica se todas as portas do sistema estão disponíveis
    ''' </summary>
    Public Function AreAllPortsAvailable() As Boolean
        Return IsPortAvailable(BACKEND_PORT) AndAlso IsPortAvailable(FRONTEND_PORT)
    End Function
    
    ''' <summary>
    ''' Obtém informações sobre as portas do sistema
    ''' </summary>
    Public Function GetSystemPortsInfo() As Dictionary(Of String, Object)
        Dim info As New Dictionary(Of String, Object)
        
        info.Add("BackendPort", BACKEND_PORT)
        info.Add("FrontendPort", FRONTEND_PORT)
        info.Add("BackendAvailable", IsPortAvailable(BACKEND_PORT))
        info.Add("FrontendAvailable", IsPortAvailable(FRONTEND_PORT))
        info.Add("AllPortsAvailable", AreAllPortsAvailable())
        
        Return info
    End Function
    
    ''' <summary>
    ''' Obtém a porta do backend
    ''' </summary>
    Public ReadOnly Property BackendPort As Integer
        Get
            Return BACKEND_PORT
        End Get
    End Property
    
    ''' <summary>
    ''' Obtém a porta do frontend
    ''' </summary>
    Public ReadOnly Property FrontendPort As Integer
        Get
            Return FRONTEND_PORT
        End Get
    End Property
End Class
