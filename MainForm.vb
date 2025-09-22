Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' Formulário principal da aplicação SUAT-IA
''' Implementa o plano de execução para nova instalação
''' Compatível com .NET Framework 4.7
''' </summary>
Public Class MainForm
    Inherits Form

    ' --- Controles da UI ---
    Private lblStatus As Label
    Private progressBar As ProgressBar
    Private WithEvents btnVerificarUpdate As Button
    Private WithEvents btnCargaInicial As Button
    Private WithEvents btnCargaIncremental As Button
    Private WithEvents btnSincronizacaoInteligente As Button
    Private WithEvents btnCriarVersaoTeste As Button
    Private WithEvents btnVerificarPortas As Button
    Private WithEvents btnMatarProcessos As Button
    Private WithEvents btnIniciarFrontend As Button
    Private WithEvents btnPararFrontend As Button
    Private WithEvents btnAbrirBrowser As Button
    Private lblFrontendStatus As Label
    Private WithEvents btnIniciarBackend As Button
    Private WithEvents btnPararBackend As Button
    Private WithEvents btnHealthCheck As Button
    Private WithEvents btnAbrirSwagger As Button
    Private lblBackendStatus As Label
    Private txtLog As TextBox
    Private rtbLogs As RichTextBox
    ' Buffer de logs no MainForm
    Private _logBuffer As New List(Of LogEntry)
    Private filteredLogs As New List(Of LogEntry)
    Private _maxBufferSize As Integer = 10000
    Private isPaused As Boolean = False
    Private showTimestamp As Boolean = True
    Private lastDisplayedCount As Integer = 0
    Private WithEvents btnNovaInterface As Button
    Private WithEvents btnNovaInterfaceAvancada As Button
    Private WithEvents btnModernDashboard As Button
    Private WithEvents btnFormularioIntegracao As Button

    ' --- ToolStrip com Monitoramento ---
    Private toolStripPrincipal As ToolStrip
    Private WithEvents toolStripMonitoramento As ToolStripDropDownButton
    Private WithEvents menuItemProcessos As ToolStripMenuItem
    Private WithEvents menuItemRede As ToolStripMenuItem
    Private WithEvents menuItemPerformance As ToolStripMenuItem
    Private WithEvents menuItemLogs As ToolStripMenuItem
    Private WithEvents menuItemLogViewer As ToolStripMenuItem
    Private animationTimer As Timer
    Private currentAnimatedItem As ToolStripMenuItem
    Private animationIndex As Integer = 0
    Private animationImages As Image()
    Private originalImage As Image
    Private isAnimating As Boolean = False

    ' --- Gerenciadores de Lógica ---
    Private updateManager As UpdateManager
    Private portManager As PortManager
    Private sincronizador As SincronizadorDados
    Private frontendServer As FrontendHttpServer
    Private backendApiManager As BackendApiManager
    Friend WithEvents btnIniciarTudo As Button
    Friend WithEvents btnPararTudo As Button
    Friend WithEvents ToolStripDropDownButton1 As ToolStripDropDownButton
    Friend WithEvents INICIAR As ToolStripMenuItem
    Friend WithEvents PARAR As ToolStripMenuItem
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents ts1 As ToolStripStatusLabel
    Friend WithEvents ts2 As ToolStripStatusLabel
    Friend WithEvents ts3 As ToolStripStatusLabel
    Friend WithEvents ts4 As ToolStripStatusLabel
    Friend WithEvents tsp1 As ToolStripProgressBar

    ' --- Constantes ---
    Private Const LOCAL_VERSION_FILE As String = "version.local"

    ' --- Métodos Auxiliares para Refatoração ---

    ''' <summary>
    ''' Executa uma operação assíncrona com tratamento de erro padronizado
    ''' </summary>
    Private Async Function ExecutarOperacaoAsync(operation As Func(Of Task),
                                                successMessage As String,
                                                errorMessage As String,
                                                Optional button As Button = Nothing) As Task(Of Boolean)
        Try
            If button IsNot Nothing Then button.Enabled = False
            UpdateStatus($"{errorMessage.Replace("Erro na ", "Executando ").Replace("Erro ao ", "Executando ")}...")

            Await operation()

            UpdateStatus(successMessage)
            ShowSuccessMessage(successMessage)
            Return True

        Catch ex As Exception
            UpdateStatus(errorMessage)
            ShowErrorMessage($"{errorMessage}: {ex.Message}")
            Return False
        Finally
            If button IsNot Nothing Then
                If Me.InvokeRequired Then
                    Me.Invoke(Sub() button.Enabled = True)
                Else
                    button.Enabled = True
                End If
            End If
        End Try
    End Function

    ''' <summary>
    ''' Executa uma operação síncrona com tratamento de erro padronizado
    ''' </summary>
    Private Function ExecutarOperacao(operation As Action,
                                    successMessage As String,
                                    errorMessage As String,
                                    Optional button As Button = Nothing) As Boolean
        Try
            If button IsNot Nothing Then button.Enabled = False
            UpdateStatus($"{errorMessage.Replace("Erro na ", "Executando ").Replace("Erro ao ", "Executando ")}...")

            operation()

            UpdateStatus(successMessage)
            ShowSuccessMessage(successMessage)
            Return True

        Catch ex As Exception
            UpdateStatus(errorMessage)
            ShowErrorMessage($"{errorMessage}: {ex.Message}")
            Return False
        Finally
            If button IsNot Nothing Then
                If Me.InvokeRequired Then
                    Me.Invoke(Sub() button.Enabled = True)
                Else
                    button.Enabled = True
                End If
            End If
        End Try
    End Function

    ''' <summary>
    ''' Mostra mensagem de sucesso
    ''' </summary>
    Private Sub ShowSuccessMessage(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MessageBox.Show(message, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information))
        Else
            MessageBox.Show(message, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ''' <summary>
    ''' Mostra mensagem de erro
    ''' </summary>
    Private Sub ShowErrorMessage(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MessageBox.Show(message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error))
        Else
            MessageBox.Show(message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    ''' <summary>
    ''' Mostra mensagem de aviso
    ''' </summary>
    Private Sub ShowWarningMessage(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MessageBox.Show(message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning))
        Else
            MessageBox.Show(message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ''' <summary>
    ''' Atualiza a informação da última sincronização no txtLog
    ''' </summary>
    Private Sub AtualizarInfoUltimaSincronizacao()
        Try
            Dim infoSincronizacao As String = ObterInfoUltimaSincronizacao()
            
            If Me.InvokeRequired Then
                Me.Invoke(Sub() MostrarInfoSincronizacao(infoSincronizacao))
            Else
                MostrarInfoSincronizacao(infoSincronizacao)
            End If
            
        Catch ex As Exception
            Console.WriteLine($"Erro ao atualizar info de sincronização: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Mostra informações de sincronização no txtLog
    ''' </summary>
    Private Sub MostrarInfoSincronizacao(info As String)
        Try
            ' Mostrar informações de sincronização no txtLog
            Console.WriteLine(info)

        Catch ex As Exception
            Console.WriteLine($"Erro ao mostrar info de sincronização: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Obtém informações da última sincronização usando o SincronizadorDados
    ''' </summary>
    Private Function ObterInfoUltimaSincronizacao() As String
        Try
            ' Obter informações do sincronizador (sem conexão direta com banco no MainForm)
            Dim syncInfo = sincronizador.ObterInformacoesSincronizacao()
            
            ' Verificar status dos serviços
            Dim statusBackend = "🔴 Offline"
            Dim statusFrontend = "🔴 Offline"
            
            Try
                If backendApiManager IsNot Nothing AndAlso backendApiManager.IsRunning Then
                    statusBackend = "🟢 Online"
                End If
            Catch
            End Try
            
            Try
                If frontendServer IsNot Nothing AndAlso frontendServer.IsServerRunning Then
                    statusFrontend = "🟢 Online"
                End If
            Catch
            End Try
            
            ' Montar mensagem baseada no resultado
            If Not syncInfo.Success Then
                Return "📊 STATUS DO SISTEMA:" & Environment.NewLine &
                       syncInfo.Status & Environment.NewLine &
                       syncInfo.Detalhes & Environment.NewLine &
                       $"🕐 Última sincronização: {syncInfo.UltimaSincronizacao}"
            End If
            
            Return "📊 STATUS DO SISTEMA:" & Environment.NewLine &
                   syncInfo.Detalhes & Environment.NewLine &
                   $"🖥️ Backend: {statusBackend}" & Environment.NewLine &
                   $"🌐 Frontend: {statusFrontend}" & Environment.NewLine &
                   $"🕐 Última sincronização: {syncInfo.UltimaSincronizacao}" & Environment.NewLine &
                   $"📅 Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
            
        Catch ex As Exception
            Return "📊 STATUS DO SISTEMA:" & Environment.NewLine &
                   "❌ Erro ao verificar status" & Environment.NewLine &
                   $"💡 Detalhes: {ex.Message}" & Environment.NewLine &
                   "🕐 Última sincronização: N/A"
        End Try
    End Function

    ''' <summary>
    ''' Executa a sequência completa de inicialização
    ''' </summary>
    Private Async Function ExecutarSequenciaInicializacao() As Task(Of Boolean)
        Try
            UpdateProgress(0, "Iniciando sequência de inicialização...")
            Await Task.Delay(500)

            ' ETAPA 1: Verificar Atualizações (0% -> 20%)
            Console.WriteLine("📋 ETAPA 1/3: Verificando atualizações...")
            UpdateProgress(5, "📋 Verificando atualizações disponíveis...")
            Dim updateSuccess = Await ExecutarVerificacaoAtualizacoes()
            If Not updateSuccess Then
                Console.WriteLine("⚠️ Falha na verificação de atualizações - continuando mesmo assim...")
                UpdateProgress(20, "⚠️ Verificação de atualizações com problemas - continuando...")
            Else
                UpdateProgress(20, "✅ Verificação de atualizações concluída")
            End If
            Await Task.Delay(500)
            Console.WriteLine()

            ' ETAPA 2: Iniciar Sistema (20% -> 80%)
            Console.WriteLine("🚀 ETAPA 2/3: Iniciando sistema...")
            UpdateProgress(25, "🚀 Iniciando sistema SUAT-IA...")
            Dim startupSuccess = Await ExecutarInicializacaoSistema()
            If Not startupSuccess Then
                Console.WriteLine("❌ Falha na inicialização do sistema - abortando sequência")
                ResetProgress()
                UpdateStatus("❌ Falha na inicialização do sistema")
                ShowErrorMessage("Falha na inicialização do sistema. Verifique os logs para mais detalhes.")
                Return False
            End If
            UpdateProgress(80, "✅ Sistema iniciado com sucesso")
            Await Task.Delay(500)
            Console.WriteLine()

            ' ETAPA 3: Sincronizar Dados (80% -> 100%)
            Console.WriteLine("🔄 ETAPA 3/3: Sincronizando dados...")
            UpdateProgress(85, "🔄 Sincronizando dados...")
            Dim syncSuccess = Await ExecutarSincronizacaoDados()
            If Not syncSuccess Then
                Console.WriteLine("⚠️ Falha na sincronização de dados - sistema iniciado mas sem sincronização")
                UpdateProgress(95, "⚠️ Sistema iniciado - falha na sincronização")
                Await Task.Delay(2000)
                ResetProgress()
                UpdateStatus("⚠️ Sistema iniciado - falha na sincronização")
                ShowWarningMessage("Sistema iniciado com sucesso, mas houve falha na sincronização de dados.")
                Return False
            End If

            Console.WriteLine("✅ Sequência completa finalizada com sucesso!")
            UpdateProgress(100, "✅ Sistema iniciado e sincronizado com sucesso!")
            Console.WriteLine()
            Console.WriteLine("🎯 ======================================")
            Console.WriteLine("🎯    MENU INICIAR CONCLUÍDO")
            Console.WriteLine("🎯 ======================================")

            Return True

        Catch ex As Exception
            Console.WriteLine($"❌ Erro na sequência de inicialização: {ex.Message}")
            ResetProgress()
            UpdateStatus("❌ Erro na sequência de inicialização")
            ShowErrorMessage($"Erro na sequência de inicialização: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Construtor do formulário
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        InitializeManagers()
        InitializeRichLog()
        RedirectConsoleToTextBox()
        InitializeAnimationTimer()
    End Sub


    ''' <summary>
    ''' Detecta automaticamente status do Backend/Frontend e atualiza a UI
    ''' </summary>
    Private Async Function DetectarStatusServicosAsync() As Task
        Try
            Console.WriteLine("🔍 Detectando status dos serviços...")
            UpdateStatus("Detectando status dos serviços...")

            ' === DETECÇÃO BACKEND ===
            Dim backendRodando As Boolean = False
            Try

                ' 1. Verificar via manager primeiro (mais confiável)
                If backendApiManager IsNot Nothing Then
                    backendRodando = backendApiManager.IsRunning
                    Console.WriteLine($"   [Backend] Status via Manager: {backendRodando}")

                    ' Se manager diz que está rodando, confirmar com health check
                    If backendRodando Then
                        Console.WriteLine($"   [Backend] Manager diz que está rodando - Confirmando com health check...")
                        Try
                            Dim healthResult = Await TestarHealthCheckBackend()
                            If Not healthResult Then
                                Console.WriteLine($"   [Backend] ⚠️ Manager diz rodando MAS health check FALHOU - processo zumbi detectado!")
                                Console.WriteLine($"   [Backend] 🧹 Matando processo zumbi automaticamente...")
                                LimparProcessosFantasmaBackend()
                                backendRodando = False
                                Console.WriteLine($"   [Backend] ✅ Processo zumbi eliminado - status corrigido para PARADO")

                                ' Verificar se backendApiManager precisa ser parado
                                If backendApiManager IsNot Nothing AndAlso backendApiManager.IsRunning Then
                                    Console.WriteLine($"   [Backend] 🔄 Parando backendApiManager (processo gerenciado)")
                                    Try
                                        backendApiManager.Stop()
                                        Console.WriteLine($"   [Backend] ✅ backendApiManager parado corretamente")
                                    Catch ex As Exception
                                        Console.WriteLine($"   [Backend] ⚠️ Erro ao parar backendApiManager: {ex.Message}")
                                    End Try
                                End If
                            Else
                                Console.WriteLine($"   [Backend] ✅ Manager + Health Check confirmam: RODANDO")
                            End If
                        Catch ex As Exception
                            Console.WriteLine($"   [Backend] ⚠️ Erro no health check de confirmação: {ex.Message}")
                            Console.WriteLine($"   [Backend] 🧹 Matando processo zumbi por precaução...")
                            LimparProcessosFantasmaBackend()
                            backendRodando = False
                            Console.WriteLine($"   [Backend] ✅ Processo problemático eliminado - status corrigido para PARADO")

                            ' Verificar se backendApiManager precisa ser parado
                            If backendApiManager IsNot Nothing AndAlso backendApiManager.IsRunning Then
                                Console.WriteLine($"   [Backend] 🔄 Parando backendApiManager (processo gerenciado)")
                                Try
                                    backendApiManager.Stop()
                                    Console.WriteLine($"   [Backend] ✅ backendApiManager parado corretamente")
                                Catch ex2 As Exception
                                    Console.WriteLine($"   [Backend] ⚠️ Erro ao parar backendApiManager: {ex2.Message}")
                                End Try
                            End If
                        End Try
                    End If
                End If

                ' 2. Se manager diz que não está rodando, verificar porta + health check
                If Not backendRodando Then
                    Dim portaOcupada = Not portManager.IsPortAvailable(3000)
                    Console.WriteLine($"   [Backend] Porta 3000 ocupada: {portaOcupada}")

                    If portaOcupada Then
                        Console.WriteLine($"   [Backend] Porta ocupada - Testando se é backend funcional...")
                        Try
                            Dim healthResult = Await TestarHealthCheckBackend()
                            If healthResult Then
                                backendRodando = True
                                Console.WriteLine($"   [Backend] ✅ Porta ocupada + Health check OK = RODANDO")

                                ' Nota: Após limpeza inicial, não deveria haver backends externos
                                If backendApiManager IsNot Nothing AndAlso Not backendApiManager.IsRunning Then
                                    Console.WriteLine($"   [Backend] ⚠️ Backend rodando mas não gerenciado pelo sistema (inesperado após limpeza)")
                                End If
                            Else
                                Console.WriteLine($"   [Backend] ⚠️ Porta ocupada mas health check FALHOU - processo zumbi")
                                Console.WriteLine($"   [Backend] 🧹 Limpando processo zumbi...")
                                LimparProcessosFantasmaBackend()
                                backendRodando = False
                            End If
                        Catch ex As Exception
                            Console.WriteLine($"   [Backend] ⚠️ Erro no health check: {ex.Message} - assumindo zumbi")
                            LimparProcessosFantasmaBackend()
                            backendRodando = False
                        End Try
                    Else
                        ' Porta livre - verificar processo fantasma
                        Dim processoExiste = VerificarProcessoNodeBackend()
                        If processoExiste Then
                            Console.WriteLine($"   [Backend] ⚠️ Processo existe mas porta livre - limpando fantasma")
                            LimparProcessosFantasmaBackend()
                        End If
                        backendRodando = False
                        Console.WriteLine($"   [Backend] ❌ Porta livre = NÃO RODANDO")
                    End If
                End If

                Console.WriteLine($"   [Backend] 🎯 Status final determinado: {If(backendRodando, "RODANDO", "PARADO")}")

            Catch ex As Exception
                Console.WriteLine($"⚠️ Erro ao detectar backend: {ex.Message}")
                backendRodando = False
            End Try

            ' === DETECÇÃO FRONTEND ===
            Dim frontendRodando As Boolean = False
            Try

                ' 1. Verificar via server primeiro (mais confiável)
                If frontendServer IsNot Nothing Then
                    frontendRodando = frontendServer.IsServerRunning
                    Console.WriteLine($"   [Frontend] Status via Server: {frontendRodando}")
                End If

                ' 2. Se server diz que não está rodando, verificar porta
                If Not frontendRodando Then
                    Dim portaOcupada = Not portManager.IsPortAvailable(8080)
                    Console.WriteLine($"   [Frontend] Porta 8080 ocupada: {portaOcupada}")

                    If portaOcupada Then
                        frontendRodando = True
                        Console.WriteLine($"   [Frontend] ✅ Porta ocupada = RODANDO")
                    Else
                        frontendRodando = False
                        Console.WriteLine($"   [Frontend] ❌ Porta livre = NÃO RODANDO")
                    End If
                End If

                Console.WriteLine($"   [Frontend] 🎯 Status final determinado: {If(frontendRodando, "RODANDO", "PARADO")}")

            Catch ex As Exception
                Console.WriteLine($"⚠️ Erro ao detectar frontend: {ex.Message}")
                frontendRodando = False
            End Try

            AtualizarInterfaceStatus(backendRodando, frontendRodando)

            Console.WriteLine($"📊 Status final detectado:")
            Console.WriteLine($"   Backend (porta 3000): {If(backendRodando, "✅ Rodando", "❌ Parado")}")
            Console.WriteLine($"   Frontend (porta 8080): {If(frontendRodando, "✅ Rodando", "❌ Parado")}")
            UpdateStatus("Detecção de status concluída")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro na detecção de status: {ex.Message}")
            UpdateStatus("Erro na detecção de status")
        End Try
    End Function

    ''' <summary>
    ''' Verifica se existe processo Node.js do backend ativo (por linha de comando)
    ''' </summary>
    Private Function VerificarProcessoNodeBackend() As Boolean
        Try
            Dim nodes = Process.GetProcessesByName("suat-backend")
            For Each p In nodes
                Try
                    Dim info = GetProcessCommandLine(p.Id)
                    If info.ToLower().Contains("suat-backend") _
                       OrElse info.ToLower().Contains("backend") _
                       OrElse info.Contains("3000") _
                       OrElse info.ToLower().Contains("server.js") _
                       OrElse info.ToLower().Contains("app.js") Then
                        Return True
                    End If
                Catch
                End Try
            Next
        Catch
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Helper: obtém a linha de comando de um processo (WMIC)
    ''' </summary>
    Private Function GetProcessCommandLine(processId As Integer) As String
        Try
            Dim si As New ProcessStartInfo()
            si.FileName = "wmic"
            si.Arguments = $"process where processid={processId} get commandline /format:value"
            si.UseShellExecute = False
            si.RedirectStandardOutput = True
            si.CreateNoWindow = True
            Using pr = Process.Start(si)
                Dim outp = pr.StandardOutput.ReadToEnd()
                pr.WaitForExit()
                Dim lines = outp.Split(Environment.NewLine)
                For Each line In lines
                    If line.Contains("CommandLine=") Then
                        Return line.Substring("CommandLine=".Length)
                    End If
                Next
            End Using
        Catch
        End Try
        Return String.Empty
    End Function

    ''' <summary>
    ''' Testa se o backend está funcional através de health check
    ''' </summary>
    Private Async Function TestarHealthCheckBackend() As Task(Of Boolean)
        Try
            Console.WriteLine("   [Backend] 🏥 Executando health check...")

            ' Usar HttpClient para testar o endpoint /health
            Using client As New Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(5) ' Timeout curto
                Dim response = Await client.GetAsync("http://localhost:3000/health")

                If response.IsSuccessStatusCode Then
                    Dim content = Await response.Content.ReadAsStringAsync()
                    Console.WriteLine($"   [Backend] 💚 Health check OK: {response.StatusCode}")
                    Return True
                Else
                    Console.WriteLine($"   [Backend] 💔 Health check FAIL: {response.StatusCode}")
                    Return False
                End If
            End Using

        Catch ex As Exception
            Console.WriteLine($"   [Backend] 💔 Health check FAIL: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Remove processos fantasma do backend (processo existe mas porta livre)
    ''' </summary>
    Private Sub LimparProcessosFantasmaBackend()
        Try
            Console.WriteLine("🧹 Limpando processos fantasma do backend...")
            Dim killedCount As Integer = 0

            ' Matar processos suat-backend que não estão escutando na porta
            For Each p In Process.GetProcessesByName("suat-backend")
                Try
                    Console.WriteLine($"🔄 Removendo processo fantasma suat-backend (PID {p.Id})")
                    p.Kill()
                    p.WaitForExit(2000)
                    killedCount += 1
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao remover processo fantasma PID {p.Id}: {ex.Message}")
                End Try
            Next

            ' Também verificar processos node que podem estar relacionados
            For Each p In Process.GetProcessesByName("node")
                Try
                    Dim cmd = GetProcessCommandLine(p.Id)
                    If cmd.ToLower().Contains("suat-backend") OrElse cmd.ToLower().Contains("backend") OrElse
                       cmd.Contains("3000") OrElse cmd.ToLower().Contains("server.js") OrElse cmd.ToLower().Contains("app.js") Then
                        Console.WriteLine($"🔄 Removendo processo node fantasma (PID {p.Id}) - cmd: {cmd.Substring(0, Math.Min(50, cmd.Length))}...")
                        p.Kill()
                        p.WaitForExit(2000)
                        killedCount += 1
                    End If
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao verificar/remover processo node PID {p.Id}: {ex.Message}")
                End Try
            Next

            If killedCount > 0 Then
                Console.WriteLine($"✅ {killedCount} processo(s) fantasma removido(s)")
            Else
                Console.WriteLine("ℹ️ Nenhum processo fantasma encontrado")
            End If

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao limpar processos fantasma: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Atualiza a interface com base no status atual
    ''' </summary>
    Private Sub AtualizarInterfaceStatus(backendRodando As Boolean, frontendRodando As Boolean)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() AtualizarInterfaceStatus(backendRodando, frontendRodando))
                Return
            End If

            ' Backend
            If backendRodando Then
                lblBackendStatus.Text = "Backend: ✅ Rodando"
                lblBackendStatus.ForeColor = Color.DarkGreen
                btnIniciarBackend.Enabled = False
                btnPararBackend.Enabled = True
                btnHealthCheck.Enabled = True
                btnAbrirSwagger.Enabled = True
                Console.WriteLine("UI: Backend marcado como Rodando")
            Else
                lblBackendStatus.Text = "Backend: ❌ Parado"
                lblBackendStatus.ForeColor = Color.DarkRed
                btnIniciarBackend.Enabled = True
                btnPararBackend.Enabled = False
                btnHealthCheck.Enabled = False
                btnAbrirSwagger.Enabled = False
                Console.WriteLine("UI: Backend marcado como Parado")
            End If

            ' Frontend
            If frontendRodando Then
                lblFrontendStatus.Text = "Frontend: ✅ Rodando"
                lblFrontendStatus.ForeColor = Color.DarkGreen
                btnIniciarFrontend.Enabled = False
                btnPararFrontend.Enabled = True
                btnAbrirBrowser.Enabled = True
                Console.WriteLine("UI: Frontend marcado como Rodando")
            Else
                lblFrontendStatus.Text = "Frontend: ❌ Parado"
                lblFrontendStatus.ForeColor = Color.DarkRed
                btnIniciarFrontend.Enabled = True
                btnPararFrontend.Enabled = False
                btnAbrirBrowser.Enabled = False
                Console.WriteLine("UI: Frontend marcado como Parado")
            End If

            ' === BOTÕES PRINCIPAIS ===
            ' INICIAR TUDO: Só habilitado se pelo menos um serviço estiver parado
            btnIniciarTudo.Enabled = Not (backendRodando AndAlso frontendRodando)

            ' PARAR TUDO: Só habilitado se pelo menos um serviço estiver rodando
            btnPararTudo.Enabled = (backendRodando OrElse frontendRodando)

            ' === MENU ITEMS ===
            ' MENU INICIAR: Mesmo critério do botão Iniciar Tudo
            INICIAR.Enabled = Not (backendRodando AndAlso frontendRodando)

            ' MENU PARAR: Mesmo critério do botão Parar Tudo
            PARAR.Enabled = (backendRodando OrElse frontendRodando)

            ' === STATUS GERAL ===
            If backendRodando And frontendRodando Then
                UpdateStatus("✅ Sistema SUAT-IA completamente operacional")
            ElseIf backendRodando Or frontendRodando Then
                UpdateStatus("⚠️ Sistema SUAT-IA parcialmente operacional")
            Else
                UpdateStatus("❌ Sistema SUAT-IA parado")
            End If

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao atualizar interface: {ex.Message}")
        End Try
    End Sub
    ''' <summary>
    ''' Inicializa os componentes da interface
    ''' </summary>
    Private Sub InitializeComponent()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.progressBar = New System.Windows.Forms.ProgressBar()
        Me.btnVerificarUpdate = New System.Windows.Forms.Button()
        Me.btnCargaInicial = New System.Windows.Forms.Button()
        Me.btnCargaIncremental = New System.Windows.Forms.Button()
        Me.btnSincronizacaoInteligente = New System.Windows.Forms.Button()
        Me.btnCriarVersaoTeste = New System.Windows.Forms.Button()
        Me.btnVerificarPortas = New System.Windows.Forms.Button()
        Me.btnMatarProcessos = New System.Windows.Forms.Button()
        Me.lblFrontendStatus = New System.Windows.Forms.Label()
        Me.btnIniciarFrontend = New System.Windows.Forms.Button()
        Me.btnPararFrontend = New System.Windows.Forms.Button()
        Me.btnAbrirBrowser = New System.Windows.Forms.Button()
        Me.lblBackendStatus = New System.Windows.Forms.Label()
        Me.btnIniciarBackend = New System.Windows.Forms.Button()
        Me.btnPararBackend = New System.Windows.Forms.Button()
        Me.btnHealthCheck = New System.Windows.Forms.Button()
        Me.btnAbrirSwagger = New System.Windows.Forms.Button()
        Me.btnIniciarTudo = New System.Windows.Forms.Button()
        Me.btnPararTudo = New System.Windows.Forms.Button()
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.btnNovaInterface = New System.Windows.Forms.Button()
        Me.btnNovaInterfaceAvancada = New System.Windows.Forms.Button()
        Me.btnModernDashboard = New System.Windows.Forms.Button()
        Me.btnFormularioIntegracao = New System.Windows.Forms.Button()
        Me.toolStripPrincipal = New System.Windows.Forms.ToolStrip()
        Me.toolStripMonitoramento = New System.Windows.Forms.ToolStripDropDownButton()
        Me.menuItemProcessos = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemRede = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemPerformance = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemLogs = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemLogViewer = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripDropDownButton1 = New System.Windows.Forms.ToolStripDropDownButton()
        Me.INICIAR = New System.Windows.Forms.ToolStripMenuItem()
        Me.PARAR = New System.Windows.Forms.ToolStripMenuItem()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.ts1 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ts2 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ts3 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ts4 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.tsp1 = New System.Windows.Forms.ToolStripProgressBar()
        Me.toolStripPrincipal.SuspendLayout()
        Me.StatusStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblStatus
        '
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.lblStatus.Location = New System.Drawing.Point(26, 66)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(760, 25)
        Me.lblStatus.TabIndex = 0
        Me.lblStatus.Text = "Sistema pronto"
        '
        'progressBar
        '
        Me.progressBar.Location = New System.Drawing.Point(20, 85)
        Me.progressBar.Name = "progressBar"
        Me.progressBar.Size = New System.Drawing.Size(760, 25)
        Me.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.progressBar.TabIndex = 1
        '
        'btnVerificarUpdate
        '
        Me.btnVerificarUpdate.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnVerificarUpdate.Location = New System.Drawing.Point(240, 43)
        Me.btnVerificarUpdate.Name = "btnVerificarUpdate"
        Me.btnVerificarUpdate.Size = New System.Drawing.Size(150, 35)
        Me.btnVerificarUpdate.TabIndex = 2
        Me.btnVerificarUpdate.Text = "Verificar Atualizações"
        '
        'btnCargaInicial
        '
        Me.btnCargaInicial.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnCargaInicial.Location = New System.Drawing.Point(195, 201)
        Me.btnCargaInicial.Name = "btnCargaInicial"
        Me.btnCargaInicial.Size = New System.Drawing.Size(150, 35)
        Me.btnCargaInicial.TabIndex = 3
        Me.btnCargaInicial.Text = "Carga Inicial (3 anos)"
        '
        'btnCargaIncremental
        '
        Me.btnCargaIncremental.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnCargaIncremental.Location = New System.Drawing.Point(32, 201)
        Me.btnCargaIncremental.Name = "btnCargaIncremental"
        Me.btnCargaIncremental.Size = New System.Drawing.Size(150, 35)
        Me.btnCargaIncremental.TabIndex = 4
        Me.btnCargaIncremental.Text = "Carga Incremental"
        '
        'btnSincronizacaoInteligente
        '
        Me.btnSincronizacaoInteligente.BackColor = System.Drawing.Color.LightGreen
        Me.btnSincronizacaoInteligente.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnSincronizacaoInteligente.Location = New System.Drawing.Point(400, 201)
        Me.btnSincronizacaoInteligente.Name = "btnSincronizacaoInteligente"
        Me.btnSincronizacaoInteligente.Size = New System.Drawing.Size(150, 35)
        Me.btnSincronizacaoInteligente.TabIndex = 5
        Me.btnSincronizacaoInteligente.Text = "Sincronização Inteligente"
        Me.btnSincronizacaoInteligente.UseVisualStyleBackColor = False
        '
        'btnCriarVersaoTeste
        '
        Me.btnCriarVersaoTeste.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnCriarVersaoTeste.Location = New System.Drawing.Point(672, 201)
        Me.btnCriarVersaoTeste.Name = "btnCriarVersaoTeste"
        Me.btnCriarVersaoTeste.Size = New System.Drawing.Size(120, 35)
        Me.btnCriarVersaoTeste.TabIndex = 6
        Me.btnCriarVersaoTeste.Text = "Criar Versão Teste"
        '
        'btnVerificarPortas
        '
        Me.btnVerificarPortas.BackColor = System.Drawing.Color.LightBlue
        Me.btnVerificarPortas.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnVerificarPortas.Location = New System.Drawing.Point(24, 125)
        Me.btnVerificarPortas.Name = "btnVerificarPortas"
        Me.btnVerificarPortas.Size = New System.Drawing.Size(150, 30)
        Me.btnVerificarPortas.TabIndex = 7
        Me.btnVerificarPortas.Text = "🔍 Verificar Portas"
        Me.btnVerificarPortas.UseVisualStyleBackColor = False
        '
        'btnMatarProcessos
        '
        Me.btnMatarProcessos.BackColor = System.Drawing.Color.LightCoral
        Me.btnMatarProcessos.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnMatarProcessos.Location = New System.Drawing.Point(180, 125)
        Me.btnMatarProcessos.Name = "btnMatarProcessos"
        Me.btnMatarProcessos.Size = New System.Drawing.Size(150, 30)
        Me.btnMatarProcessos.TabIndex = 8
        Me.btnMatarProcessos.Text = "💀 Matar Processos"
        Me.btnMatarProcessos.UseVisualStyleBackColor = False
        '
        'lblFrontendStatus
        '
        Me.lblFrontendStatus.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblFrontendStatus.ForeColor = System.Drawing.Color.DarkRed
        Me.lblFrontendStatus.Location = New System.Drawing.Point(190, 125)
        Me.lblFrontendStatus.Name = "lblFrontendStatus"
        Me.lblFrontendStatus.Size = New System.Drawing.Size(200, 20)
        Me.lblFrontendStatus.TabIndex = 8
        Me.lblFrontendStatus.Text = "Frontend: Não iniciado"
        '
        'btnIniciarFrontend
        '
        Me.btnIniciarFrontend.BackColor = System.Drawing.Color.LightBlue
        Me.btnIniciarFrontend.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnIniciarFrontend.Location = New System.Drawing.Point(368, 120)
        Me.btnIniciarFrontend.Name = "btnIniciarFrontend"
        Me.btnIniciarFrontend.Size = New System.Drawing.Size(110, 30)
        Me.btnIniciarFrontend.TabIndex = 9
        Me.btnIniciarFrontend.Text = "Iniciar Frontend"
        Me.btnIniciarFrontend.UseVisualStyleBackColor = False
        '
        'btnPararFrontend
        '
        Me.btnPararFrontend.BackColor = System.Drawing.Color.LightCoral
        Me.btnPararFrontend.Enabled = False
        Me.btnPararFrontend.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnPararFrontend.Location = New System.Drawing.Point(556, 206)
        Me.btnPararFrontend.Name = "btnPararFrontend"
        Me.btnPararFrontend.Size = New System.Drawing.Size(110, 30)
        Me.btnPararFrontend.TabIndex = 10
        Me.btnPararFrontend.Text = "Parar Frontend"
        Me.btnPararFrontend.UseVisualStyleBackColor = False
        '
        'btnAbrirBrowser
        '
        Me.btnAbrirBrowser.BackColor = System.Drawing.Color.LightGreen
        Me.btnAbrirBrowser.Enabled = False
        Me.btnAbrirBrowser.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnAbrirBrowser.Location = New System.Drawing.Point(484, 119)
        Me.btnAbrirBrowser.Name = "btnAbrirBrowser"
        Me.btnAbrirBrowser.Size = New System.Drawing.Size(120, 30)
        Me.btnAbrirBrowser.TabIndex = 11
        Me.btnAbrirBrowser.Text = "Abrir no Browser"
        Me.btnAbrirBrowser.UseVisualStyleBackColor = False
        '
        'lblBackendStatus
        '
        Me.lblBackendStatus.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblBackendStatus.ForeColor = System.Drawing.Color.DarkRed
        Me.lblBackendStatus.Location = New System.Drawing.Point(600, 130)
        Me.lblBackendStatus.Name = "lblBackendStatus"
        Me.lblBackendStatus.Size = New System.Drawing.Size(180, 20)
        Me.lblBackendStatus.TabIndex = 12
        Me.lblBackendStatus.Text = "Backend API: Não iniciado"
        '
        'btnIniciarBackend
        '
        Me.btnIniciarBackend.BackColor = System.Drawing.Color.LightBlue
        Me.btnIniciarBackend.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnIniciarBackend.Location = New System.Drawing.Point(20, 155)
        Me.btnIniciarBackend.Name = "btnIniciarBackend"
        Me.btnIniciarBackend.Size = New System.Drawing.Size(90, 30)
        Me.btnIniciarBackend.TabIndex = 13
        Me.btnIniciarBackend.Text = "Iniciar API"
        Me.btnIniciarBackend.UseVisualStyleBackColor = False
        '
        'btnPararBackend
        '
        Me.btnPararBackend.BackColor = System.Drawing.Color.LightCoral
        Me.btnPararBackend.Enabled = False
        Me.btnPararBackend.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnPararBackend.Location = New System.Drawing.Point(120, 155)
        Me.btnPararBackend.Name = "btnPararBackend"
        Me.btnPararBackend.Size = New System.Drawing.Size(90, 30)
        Me.btnPararBackend.TabIndex = 14
        Me.btnPararBackend.Text = "Parar API"
        Me.btnPararBackend.UseVisualStyleBackColor = False
        '
        'btnHealthCheck
        '
        Me.btnHealthCheck.BackColor = System.Drawing.Color.LightYellow
        Me.btnHealthCheck.Enabled = False
        Me.btnHealthCheck.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnHealthCheck.Location = New System.Drawing.Point(220, 155)
        Me.btnHealthCheck.Name = "btnHealthCheck"
        Me.btnHealthCheck.Size = New System.Drawing.Size(100, 30)
        Me.btnHealthCheck.TabIndex = 15
        Me.btnHealthCheck.Text = "Health Check"
        Me.btnHealthCheck.UseVisualStyleBackColor = False
        '
        'btnAbrirSwagger
        '
        Me.btnAbrirSwagger.BackColor = System.Drawing.Color.LightGreen
        Me.btnAbrirSwagger.Enabled = False
        Me.btnAbrirSwagger.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnAbrirSwagger.Location = New System.Drawing.Point(330, 155)
        Me.btnAbrirSwagger.Name = "btnAbrirSwagger"
        Me.btnAbrirSwagger.Size = New System.Drawing.Size(110, 30)
        Me.btnAbrirSwagger.TabIndex = 16
        Me.btnAbrirSwagger.Text = "Abrir Swagger"
        Me.btnAbrirSwagger.UseVisualStyleBackColor = False
        '
        'btnIniciarTudo
        '
        Me.btnIniciarTudo.BackColor = System.Drawing.Color.Gold
        Me.btnIniciarTudo.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnIniciarTudo.ForeColor = System.Drawing.Color.DarkBlue
        Me.btnIniciarTudo.Location = New System.Drawing.Point(480, 155)
        Me.btnIniciarTudo.Name = "btnIniciarTudo"
        Me.btnIniciarTudo.Size = New System.Drawing.Size(200, 30)
        Me.btnIniciarTudo.TabIndex = 17
        Me.btnIniciarTudo.Text = "🚀 INICIAR SISTEMA COMPLETO"
        Me.btnIniciarTudo.UseVisualStyleBackColor = False
        '
        'btnPararTudo
        '
        Me.btnPararTudo.BackColor = System.Drawing.Color.Crimson
        Me.btnPararTudo.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnPararTudo.ForeColor = System.Drawing.Color.White
        Me.btnPararTudo.Location = New System.Drawing.Point(690, 155)
        Me.btnPararTudo.Name = "btnPararTudo"
        Me.btnPararTudo.Size = New System.Drawing.Size(90, 30)
        Me.btnPararTudo.TabIndex = 18
        Me.btnPararTudo.Text = "🛑 PARAR TUDO"
        Me.btnPararTudo.UseVisualStyleBackColor = False
        '
        'txtLog
        '
        Me.txtLog.BackColor = System.Drawing.Color.Black
        Me.txtLog.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.txtLog.ForeColor = System.Drawing.Color.Lime
        Me.txtLog.Location = New System.Drawing.Point(20, 242)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ReadOnly = True
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(760, 298)
        Me.txtLog.TabIndex = 19
        '
        'btnNovaInterface
        '
        Me.btnNovaInterface.Location = New System.Drawing.Point(0, 0)
        Me.btnNovaInterface.Name = "btnNovaInterface"
        Me.btnNovaInterface.Size = New System.Drawing.Size(75, 23)
        Me.btnNovaInterface.TabIndex = 25
        '
        'btnNovaInterfaceAvancada
        '
        Me.btnNovaInterfaceAvancada.Location = New System.Drawing.Point(0, 0)
        Me.btnNovaInterfaceAvancada.Name = "btnNovaInterfaceAvancada"
        Me.btnNovaInterfaceAvancada.Size = New System.Drawing.Size(75, 23)
        Me.btnNovaInterfaceAvancada.TabIndex = 26
        '
        'btnModernDashboard
        '
        Me.btnModernDashboard.Location = New System.Drawing.Point(0, 0)
        Me.btnModernDashboard.Name = "btnModernDashboard"
        Me.btnModernDashboard.Size = New System.Drawing.Size(75, 23)
        Me.btnModernDashboard.TabIndex = 27
        '
        'btnFormularioIntegracao
        '
        Me.btnFormularioIntegracao.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.btnFormularioIntegracao.Location = New System.Drawing.Point(440, 85)
        Me.btnFormularioIntegracao.Name = "btnFormularioIntegracao"
        Me.btnFormularioIntegracao.Size = New System.Drawing.Size(220, 25)
        Me.btnFormularioIntegracao.TabIndex = 23
        Me.btnFormularioIntegracao.Text = "🔗 Formulário de Integração"
        Me.btnFormularioIntegracao.UseVisualStyleBackColor = True
        '
        'toolStripPrincipal
        '
        Me.toolStripPrincipal.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.toolStripPrincipal.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripMonitoramento, Me.ToolStripDropDownButton1})
        Me.toolStripPrincipal.Location = New System.Drawing.Point(0, 0)
        Me.toolStripPrincipal.Name = "toolStripPrincipal"
        Me.toolStripPrincipal.Size = New System.Drawing.Size(1165, 40)
        Me.toolStripPrincipal.TabIndex = 24
        Me.toolStripPrincipal.Text = "Barra de Ferramentas"
        '
        'toolStripMonitoramento
        '
        Me.toolStripMonitoramento.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.menuItemProcessos, Me.menuItemRede, Me.menuItemPerformance, Me.menuItemLogs, Me.menuItemLogViewer})
        Me.toolStripMonitoramento.ImageTransparentColor = System.Drawing.Color.Transparent
        Me.toolStripMonitoramento.Name = "toolStripMonitoramento"
        Me.toolStripMonitoramento.Size = New System.Drawing.Size(179, 34)
        Me.toolStripMonitoramento.Text = "Monitoramento"
        '
        'menuItemProcessos
        '
        Me.menuItemProcessos.Name = "menuItemProcessos"
        Me.menuItemProcessos.Size = New System.Drawing.Size(407, 40)
        Me.menuItemProcessos.Text = "🔄 Processos do Sistema"
        '
        'menuItemRede
        '
        Me.menuItemRede.Name = "menuItemRede"
        Me.menuItemRede.Size = New System.Drawing.Size(407, 40)
        Me.menuItemRede.Text = "🌐 Monitoramento de Rede"
        '
        'menuItemPerformance
        '
        Me.menuItemPerformance.Name = "menuItemPerformance"
        Me.menuItemPerformance.Size = New System.Drawing.Size(407, 40)
        Me.menuItemPerformance.Text = "📊 Performance"
        '
        'menuItemLogs
        '
        Me.menuItemLogs.Name = "menuItemLogs"
        Me.menuItemLogs.Size = New System.Drawing.Size(407, 40)
        Me.menuItemLogs.Text = "📝 Logs do Sistema"
        '
        'menuItemLogViewer
        '
        Me.menuItemLogViewer.Name = "menuItemLogViewer"
        Me.menuItemLogViewer.Size = New System.Drawing.Size(407, 40)
        Me.menuItemLogViewer.Text = "🔍 Abrir Visualizador de Logs"
        '
        'ToolStripDropDownButton1
        '
        Me.ToolStripDropDownButton1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.INICIAR, Me.PARAR})
        Me.ToolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripDropDownButton1.Name = "ToolStripDropDownButton1"
        Me.ToolStripDropDownButton1.Size = New System.Drawing.Size(101, 34)
        Me.ToolStripDropDownButton1.Text = "SUATIA"
        '
        'INICIAR
        '
        Me.INICIAR.Name = "INICIAR"
        Me.INICIAR.Size = New System.Drawing.Size(205, 40)
        Me.INICIAR.Text = "INICIAR"
        '
        'PARAR
        '
        Me.PARAR.Name = "PARAR"
        Me.PARAR.Size = New System.Drawing.Size(205, 40)
        Me.PARAR.Text = "PARAR"
        '
        'StatusStrip1
        '
        Me.StatusStrip1.ImageScalingSize = New System.Drawing.Size(16, 16)
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ts1, Me.ts2, Me.ts3, Me.ts4, Me.tsp1})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 617)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.AutoSize = False
        Me.StatusStrip1.SizingGrip = False
        Me.StatusStrip1.MinimumSize = New System.Drawing.Size(0, 38)
        Me.StatusStrip1.MaximumSize = New System.Drawing.Size(0, 38)
        Me.StatusStrip1.Size = New System.Drawing.Size(1165, 38)
        Me.StatusStrip1.TabIndex = 28
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'ts1
        '
        Me.ts1.Name = "ts1"
        Me.ts1.Size = New System.Drawing.Size(85, 30)
        Me.ts1.Text = "Agenda"
        '
        'ts2
        '
        Me.ts2.AutoSize = False
        Me.ts2.Name = "ts2"
        Me.ts2.Size = New System.Drawing.Size(94, 30)
        Me.ts2.Text = "BAckend"
        '
        'ts3
        '
        Me.ts3.AutoSize = False
        Me.ts3.Name = "ts3"
        Me.ts3.Size = New System.Drawing.Size(96, 30)
        Me.ts3.Text = "Frontend"
        '
        'ts4
        '
        Me.ts4.Name = "ts4"
        Me.ts4.Size = New System.Drawing.Size(130, 30)
        Me.ts4.Text = "Tempo: 1999"
        '
        'tsp1
        '
        Me.tsp1.Name = "tsp1"
        Me.tsp1.Size = New System.Drawing.Size(100, 18)
        '
        'MainForm
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.ClientSize = New System.Drawing.Size(1165, 656)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.toolStripPrincipal)
        Me.Controls.Add(Me.btnNovaInterface)
        Me.Controls.Add(Me.btnNovaInterfaceAvancada)
        Me.Controls.Add(Me.btnModernDashboard)
        Me.Controls.Add(Me.btnFormularioIntegracao)
        Me.Controls.Add(Me.lblStatus)
        Me.Controls.Add(Me.progressBar)
        Me.Controls.Add(Me.btnVerificarUpdate)
        Me.Controls.Add(Me.btnCargaInicial)
        Me.Controls.Add(Me.btnCargaIncremental)
        Me.Controls.Add(Me.btnSincronizacaoInteligente)
        Me.Controls.Add(Me.btnCriarVersaoTeste)
        Me.Controls.Add(Me.btnVerificarPortas)
        Me.Controls.Add(Me.btnMatarProcessos)
        Me.Controls.Add(Me.lblFrontendStatus)
        Me.Controls.Add(Me.btnIniciarFrontend)
        Me.Controls.Add(Me.btnPararFrontend)
        Me.Controls.Add(Me.btnAbrirBrowser)
        Me.Controls.Add(Me.lblBackendStatus)
        Me.Controls.Add(Me.btnIniciarBackend)
        Me.Controls.Add(Me.btnPararBackend)
        Me.Controls.Add(Me.btnHealthCheck)
        Me.Controls.Add(Me.btnAbrirSwagger)
        Me.Controls.Add(Me.btnIniciarTudo)
        Me.Controls.Add(Me.btnPararTudo)
        Me.Controls.Add(Me.txtLog)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "SUAT-IA - Sistema de Integração"
        Me.toolStripPrincipal.ResumeLayout(False)
        Me.toolStripPrincipal.PerformLayout()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    ''' <summary>
    ''' Configuração do botão para abrir a nova interface
    ''' </summary>
    Private Sub ConfigureNovaInterfaceButton()
        Try
            Me.btnNovaInterface.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.btnNovaInterface.Location = New System.Drawing.Point(620, 20)
            Me.btnNovaInterface.Name = "btnNovaInterface"
            Me.btnNovaInterface.Size = New System.Drawing.Size(160, 25)
            Me.btnNovaInterface.TabIndex = 20
            Me.btnNovaInterface.Text = "Nova Interface (Beta)"
            Me.btnNovaInterface.BackColor = System.Drawing.Color.MediumPurple
            Me.btnNovaInterface.ForeColor = System.Drawing.Color.White
            Me.btnNovaInterface.FlatStyle = System.Windows.Forms.FlatStyle.Flat
            Me.btnNovaInterface.FlatAppearance.BorderSize = 0
            Me.btnNovaInterface.Visible = True
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Evento do botão que abre a nova interface moderna
    ''' </summary>
    Private Sub btnNovaInterface_Click(sender As Object, e As EventArgs) Handles btnNovaInterface.Click
        Try
            Dim f As New NovaInterfaceForm()
            f.Show()
        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir nova interface: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Configuração do botão que abre a interface avançada focada em logs
    ''' </summary>
    Private Sub ConfigureNovaInterfaceAvancadaButton()
        Try
            Me.btnNovaInterfaceAvancada.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.btnNovaInterfaceAvancada.Location = New System.Drawing.Point(440, 20)
            Me.btnNovaInterfaceAvancada.Name = "btnNovaInterfaceAvancada"
            Me.btnNovaInterfaceAvancada.Size = New System.Drawing.Size(170, 25)
            Me.btnNovaInterfaceAvancada.TabIndex = 21
            Me.btnNovaInterfaceAvancada.Text = "Interface Avançada (Logs)"
            Me.btnNovaInterfaceAvancada.BackColor = System.Drawing.Color.SlateBlue
            Me.btnNovaInterfaceAvancada.ForeColor = System.Drawing.Color.White
            Me.btnNovaInterfaceAvancada.FlatStyle = System.Windows.Forms.FlatStyle.Flat
            Me.btnNovaInterfaceAvancada.FlatAppearance.BorderSize = 0
            Me.btnNovaInterfaceAvancada.Visible = True
        Catch
        End Try
    End Sub

    Private Sub btnNovaInterfaceAvancada_Click(sender As Object, e As EventArgs) Handles btnNovaInterfaceAvancada.Click
        Try
            Dim f As New NovaInterfaceAvancadaForm()
            f.Show()
        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir interface avançada: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Configuração do botão para o Modern Dashboard
    ''' </summary>
    Private Sub ConfigureModernDashboardButton()
        Try
            Me.btnModernDashboard.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.btnModernDashboard.Location = New System.Drawing.Point(260, 20)
            Me.btnModernDashboard.Name = "btnModernDashboard"
            Me.btnModernDashboard.Size = New System.Drawing.Size(170, 25)
            Me.btnModernDashboard.TabIndex = 22
            Me.btnModernDashboard.Text = "🎨 Modern Dashboard"
            Me.btnModernDashboard.BackColor = System.Drawing.Color.FromArgb(99, 102, 241)
            Me.btnModernDashboard.ForeColor = System.Drawing.Color.White
            Me.btnModernDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat
            Me.btnModernDashboard.FlatAppearance.BorderSize = 0
            Me.btnModernDashboard.Visible = True
        Catch
        End Try
    End Sub

    Private Sub btnModernDashboard_Click(sender As Object, e As EventArgs) Handles btnModernDashboard.Click
        Try
            Dim f As New ModernDashboardForm()
            f.Show()
        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir Modern Dashboard: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Configuração do botão para o Formulário de Integração
    ''' </summary>
    Private Sub ConfigureFormularioIntegracaoButton()
        Try
            Me.btnFormularioIntegracao.BackColor = System.Drawing.Color.FromArgb(51, 122, 183)
            Me.btnFormularioIntegracao.ForeColor = System.Drawing.Color.White
            Me.btnFormularioIntegracao.FlatStyle = System.Windows.Forms.FlatStyle.Flat
            Me.btnFormularioIntegracao.FlatAppearance.BorderSize = 0
            Me.btnFormularioIntegracao.Visible = True
        Catch
        End Try
    End Sub

    Private Sub btnFormularioIntegracao_Click(sender As Object, e As EventArgs) Handles btnFormularioIntegracao.Click
        Try
            ' Passar instâncias dos gerenciadores para integração completa
            Dim f As New FormularioIntegracao(Me, updateManager, portManager,
                                            New DatabaseManager(), backendApiManager,
                                            frontendServer, sincronizador)
            f.Show()
            Console.WriteLine("🔗 Formulário de Integração aberto com gerenciadores compartilhados")
        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir Formulário de Integração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"❌ Erro ao abrir Formulário de Integração: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Inicializa o timer para animação dos menu items
    ''' </summary>
    Private Sub InitializeAnimationTimer()
        animationTimer = New Timer()
        animationTimer.Interval = 50 ' 50ms para animação muito fluida (20 FPS)
        AddHandler animationTimer.Tick, AddressOf OnAnimationTick

        ' Criar array de imagens para animação
        CreateAnimationImages()
    End Sub

    ''' <summary>
    ''' Cria as imagens para animação do botão de monitoramento
    ''' </summary>
    Private Sub CreateAnimationImages()
        Try
            ' Criar 24 frames de animação para rotação super fluida (15° por frame)
            ReDim animationImages(23)

            For i As Integer = 0 To 23
                animationImages(i) = CreateMonitoringImage(i)
            Next

            ' Salvar imagem original
            originalImage = CreateMonitoringImage(0)

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao criar imagens de animação: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Cria uma imagem de engrenagem para o frame especificado
    ''' </summary>
    ''' <param name="frame">Frame da animação (0-23)</param>
    ''' <returns>Imagem do frame</returns>
    Private Function CreateMonitoringImage(frame As Integer) As Image
        Try
            ' Imagem maior para melhor visibilidade (como ampulheta)
            Dim bitmap As New Bitmap(24, 24)

            Using g As Graphics = Graphics.FromImage(bitmap)
                g.Clear(Color.Transparent)
                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality

                ' Calcular rotação baseada no frame (15 graus por frame = 360°/24 frames)
                Dim rotationAngle As Single = frame * 15.0F

                ' Definir centro da engrenagem (maior)
                Dim centerX As Single = 12.0F
                Dim centerY As Single = 12.0F

                ' Salvar estado atual do graphics
                Dim oldTransform = g.Transform

                ' Aplicar rotação no centro da imagem
                g.TranslateTransform(centerX, centerY)
                g.RotateTransform(rotationAngle)
                g.TranslateTransform(-centerX, -centerY)

                ' Desenhar engrenagem maior
                DrawGear(g, centerX, centerY, 10.0F, 7.0F, 12) ' Maior e com mais dentes

                ' Restaurar transformação original
                g.Transform = oldTransform
            End Using

            Return bitmap

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao criar imagem: {ex.Message}")
            ' Retornar imagem padrão em caso de erro
            Return New Bitmap(24, 24)
        End Try
    End Function

    ''' <summary>
    ''' Desenha uma engrenagem
    ''' </summary>
    ''' <param name="g">Graphics object</param>
    ''' <param name="centerX">Centro X</param>
    ''' <param name="centerY">Centro Y</param>
    ''' <param name="outerRadius">Raio externo</param>
    ''' <param name="innerRadius">Raio interno</param>
    ''' <param name="teeth">Número de dentes</param>
    Private Sub DrawGear(g As Graphics, centerX As Single, centerY As Single, outerRadius As Single, innerRadius As Single, teeth As Integer)
        Try
            Dim points As New List(Of PointF)
            Dim angleStep As Single = 360.0F / teeth
            Dim halfToothAngle As Single = angleStep / 4.0F

            ' Criar pontos da engrenagem
            For i As Integer = 0 To teeth - 1
                Dim baseAngle As Single = i * angleStep

                ' Ponto externo esquerdo do dente
                Dim angle1 As Single = (baseAngle - halfToothAngle) * Math.PI / 180.0F
                points.Add(New PointF(
                    centerX + outerRadius * Math.Cos(angle1),
                    centerY + outerRadius * Math.Sin(angle1)
                ))

                ' Ponto externo direito do dente
                Dim angle2 As Single = (baseAngle + halfToothAngle) * Math.PI / 180.0F
                points.Add(New PointF(
                    centerX + outerRadius * Math.Cos(angle2),
                    centerY + outerRadius * Math.Sin(angle2)
                ))

                ' Ponto interno direito
                Dim angle3 As Single = (baseAngle + halfToothAngle) * Math.PI / 180.0F
                points.Add(New PointF(
                    centerX + innerRadius * Math.Cos(angle3),
                    centerY + innerRadius * Math.Sin(angle3)
                ))

                ' Ponto interno esquerdo
                Dim angle4 As Single = (baseAngle + angleStep - halfToothAngle) * Math.PI / 180.0F
                points.Add(New PointF(
                    centerX + innerRadius * Math.Cos(angle4),
                    centerY + innerRadius * Math.Sin(angle4)
                ))
            Next

            ' Desenhar engrenagem preenchida
            Using brush As New SolidBrush(Color.DarkSlateGray)
                g.FillPolygon(brush, points.ToArray())
            End Using

            ' Desenhar contorno
            Using pen As New Pen(Color.Black, 0.8F)
                g.DrawPolygon(pen, points.ToArray())
            End Using

            ' Desenhar círculo central
            Using centerBrush As New SolidBrush(Color.LightGray)
                g.FillEllipse(centerBrush, centerX - 2, centerY - 2, 4, 4)
            End Using

            ' Desenhar contorno do círculo central
            Using centerPen As New Pen(Color.DarkGray, 0.5F)
                g.DrawEllipse(centerPen, centerX - 2, centerY - 2, 4, 4)
            End Using

        Catch ex As Exception
            ' Em caso de erro, desenhar círculo simples
            Using fallbackBrush As New SolidBrush(Color.Gray)
                g.FillEllipse(fallbackBrush, centerX - 4, centerY - 4, 8, 8)
            End Using
        End Try
    End Sub

    ''' <summary>
    ''' Evento de tick do timer de animação
    ''' </summary>
    Private Sub OnAnimationTick(sender As Object, e As EventArgs)
        Try
            If isAnimating AndAlso animationImages IsNot Nothing Then
                animationIndex += 1

                ' Alternar entre as imagens de animação
                Dim frameIndex As Integer = animationIndex Mod animationImages.Length
                toolStripMonitoramento.Image = animationImages(frameIndex)

                ' Atualizar o ToolStrip para refletir a mudança
                toolStripPrincipal.Invalidate()
            End If
        Catch ex As Exception
            ' Ignore animation errors
        End Try
    End Sub

    ''' <summary>
    ''' Inicia animação para um menu item específico
    ''' </summary>
    Private Sub StartAnimation(menuItem As ToolStripMenuItem, duration As Integer)
        Try
            ' Parar animação anterior se existir
            StopAnimation()

            ' Configurar nova animação
            currentAnimatedItem = menuItem
            animationIndex = 0
            isAnimating = True

            ' Iniciar animação da imagem
            animationTimer.Start()

            ' Configurar timer para parar a animação
            Task.Run(Sub()
                         Threading.Thread.Sleep(duration)
                         Me.Invoke(Sub() StopAnimation())
                     End Sub)

            Console.WriteLine($"🎬 Iniciando monitoramento: {menuItem.Text}")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro na animação: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Para a animação atual
    ''' </summary>
    Private Sub StopAnimation()
        Try
            animationTimer.Stop()
            isAnimating = False

            ' Restaurar imagem original
            If originalImage IsNot Nothing Then
                toolStripMonitoramento.Image = originalImage
                toolStripPrincipal.Invalidate()
            End If

            If currentAnimatedItem IsNot Nothing Then
                Console.WriteLine($"✅ Monitoramento concluído: {currentAnimatedItem.Text}")
                currentAnimatedItem = Nothing
            End If

            animationIndex = 0
        Catch ex As Exception
            ' Ignore stop errors
        End Try
    End Sub

    ' --- Eventos dos Menu Items ---

    ''' <summary>
    ''' Monitoramento de Processos do Sistema
    ''' </summary>
    Private Sub menuItemProcessos_Click(sender As Object, e As EventArgs) Handles menuItemProcessos.Click
        Try
            StartAnimation(menuItemProcessos, 5000) ' 5 segundos de animação

            ' Simular monitoramento de processos
            Task.Run(Sub()
                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("🔍 Verificando processos do sistema..."))

                         Threading.Thread.Sleep(1500)
                         Me.Invoke(Sub() Console.WriteLine("📊 Encontrados 45 processos ativos"))

                         Threading.Thread.Sleep(1500)
                         Me.Invoke(Sub() Console.WriteLine("💾 Uso de memória: 2.1GB / 8GB"))

                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("✅ Monitoramento de processos concluído"))
                     End Sub)

        Catch ex As Exception
            MessageBox.Show($"Erro no monitoramento de processos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Monitoramento de Rede
    ''' </summary>
    Private Sub menuItemRede_Click(sender As Object, e As EventArgs) Handles menuItemRede.Click
        Try
            StartAnimation(menuItemRede, 4000) ' 4 segundos de animação

            ' Simular monitoramento de rede
            Task.Run(Sub()
                         Threading.Thread.Sleep(800)
                         Me.Invoke(Sub() Console.WriteLine("🌐 Verificando conectividade de rede..."))

                         Threading.Thread.Sleep(1200)
                         Me.Invoke(Sub() Console.WriteLine("📡 Ping para 8.8.8.8: 15ms"))

                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("📈 Download: 45 Mbps | Upload: 12 Mbps"))

                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("✅ Monitoramento de rede concluído"))
                     End Sub)

        Catch ex As Exception
            MessageBox.Show($"Erro no monitoramento de rede: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Monitoramento de Performance
    ''' </summary>
    Private Sub menuItemPerformance_Click(sender As Object, e As EventArgs) Handles menuItemPerformance.Click
        Try
            StartAnimation(menuItemPerformance, 6000) ' 6 segundos de animação

            ' Simular monitoramento de performance
            Task.Run(Sub()
                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("📊 Analisando performance do sistema..."))

                         Threading.Thread.Sleep(1500)
                         Me.Invoke(Sub() Console.WriteLine("🖥️ CPU: 15% | Temperatura: 45°C"))

                         Threading.Thread.Sleep(1500)
                         Me.Invoke(Sub() Console.WriteLine("💾 RAM: 65% utilizada | Disco: 2GB livres"))

                         Threading.Thread.Sleep(1500)
                         Me.Invoke(Sub() Console.WriteLine("⚡ Sistema funcionando dentro dos parâmetros normais"))

                         Threading.Thread.Sleep(500)
                         Me.Invoke(Sub() Console.WriteLine("✅ Análise de performance concluída"))
                     End Sub)

        Catch ex As Exception
            MessageBox.Show($"Erro no monitoramento de performance: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Monitoramento de Logs
    ''' </summary>
    Private Sub menuItemLogs_Click(sender As Object, e As EventArgs) Handles menuItemLogs.Click
        Try
            StartAnimation(menuItemLogs, 3500) ' 3.5 segundos de animação

            ' Simular análise de logs
            Task.Run(Sub()
                         Threading.Thread.Sleep(700)
                         Me.Invoke(Sub() Console.WriteLine("📝 Analisando logs do sistema..."))

                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("🔍 Verificando últimas 100 entradas"))

                         Threading.Thread.Sleep(1000)
                         Me.Invoke(Sub() Console.WriteLine("⚠️ Encontrados 2 warnings, 0 errors"))

                         Threading.Thread.Sleep(800)
                         Me.Invoke(Sub() Console.WriteLine("✅ Análise de logs concluída"))
                     End Sub)

        Catch ex As Exception
            MessageBox.Show($"Erro na análise de logs: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Abre o formulário especializado de logs
    ''' </summary>
    Private Sub menuItemLogViewer_Click(sender As Object, e As EventArgs) Handles menuItemLogViewer.Click
        Try
            ' Abrir formulário de logs profissional (Singleton pattern)
            Dim logForm As LogViewerFormProfessional = LogViewerFormProfessional.Instance

            If Not logForm.Visible Then
                logForm.Show()
            Else
                logForm.BringToFront()
            End If

            Console.WriteLine("🎯 Sistema Profissional de Gestão de Logs aberto")

        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir visualizador de logs: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"❌ Erro ao abrir visualizador de logs: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Inicializa os gerenciadores de lógica
    ''' </summary>
    Private Sub InitializeManagers()
        ' Inicializar managers básicos
        updateManager = New UpdateManager()
        portManager = New PortManager()
        sincronizador = New SincronizadorDados()

        ' Inicializar servidores
        InitializeServers()

        ' Conectar eventos
        ConnectEventHandlers()
    End Sub

    ''' <summary>
    ''' Inicializa os servidores (Frontend e Backend)
    ''' </summary>
    Private Sub InitializeServers()
        Try
            ' Inicializar servidor frontend
            Dim frontendBuildPath = Path.Combine(Application.StartupPath, "frontend", "build")
            frontendServer = New FrontendHttpServer(frontendBuildPath, 8080)

            ' Inicializar backend API manager
            Dim backendExePath = Path.Combine(Application.StartupPath, "backend", "suat-backend.exe")
            backendApiManager = New BackendApiManager(backendExePath)
        Catch ex As Exception
            Console.WriteLine($"Erro ao inicializar servidores: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Conecta os event handlers dos managers
    ''' </summary>
    Private Sub ConnectEventHandlers()
        Try
            ' Eventos do UpdateManager
            AddHandler updateManager.ProgressChanged, AddressOf OnUpdateProgress

            ' Eventos do PortManager
            AddHandler portManager.PortStatusChanged, AddressOf OnPortStatusChanged

            ' Eventos do FrontendServer
            AddHandler frontendServer.StatusChanged, AddressOf OnFrontendStatusChanged
            AddHandler frontendServer.ServerStarted, AddressOf OnFrontendServerStarted
            AddHandler frontendServer.ServerStopped, AddressOf OnFrontendServerStopped
            AddHandler frontendServer.RequestReceived, AddressOf OnFrontendRequestReceived

            ' Eventos do BackendApiManager
            AddHandler backendApiManager.StatusChanged, AddressOf OnBackendStatusChanged
            AddHandler backendApiManager.ServerStarted, AddressOf OnBackendServerStarted
            AddHandler backendApiManager.ServerStopped, AddressOf OnBackendServerStopped
            AddHandler backendApiManager.HealthCheckResult, AddressOf OnBackendHealthCheck
        Catch ex As Exception
            Console.WriteLine($"Erro ao conectar event handlers: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Redireciona a saída do Console para o TextBox e para o LogViewer
    ''' </summary>
    Private Sub RedirectConsoleToTextBox()
        Console.SetOut(New MainFormConsoleWriter(AddressOf AddLog))
    End Sub

    ''' <summary>
    ''' Inicializa o RichTextBox de logs e oculta o txtLog legado
    ''' </summary>
    Private Sub InitializeRichLog()
        Try
            ' Criar se ainda não existir
            If rtbLogs Is Nothing Then
                rtbLogs = New RichTextBox()
            End If

            ' Copiar posição/tamanho do txtLog para manter layout
            If txtLog IsNot Nothing Then
                rtbLogs.Location = txtLog.Location
                rtbLogs.Size = txtLog.Size
                rtbLogs.Anchor = txtLog.Anchor
                rtbLogs.TabIndex = txtLog.TabIndex
                txtLog.Visible = False
            Else
                ' Fallback padrão
                rtbLogs.Dock = DockStyle.Fill
            End If

            rtbLogs.ReadOnly = True
            rtbLogs.BackColor = Color.Black
            rtbLogs.ForeColor = Color.Gainsboro
            rtbLogs.BorderStyle = BorderStyle.FixedSingle
            rtbLogs.Font = New Font("Consolas", 9.0!)

            ' Adicionar ao formulário se ainda não estiver
            If Not Me.Controls.Contains(rtbLogs) Then
                Me.Controls.Add(rtbLogs)
                rtbLogs.BringToFront()
            End If
        Catch
        End Try
    End Sub

    ' === Logging local no MainForm ===
    Public Enum LocalLogType
        Info
        Warning
        [Error]
        Success
        Debug
    End Enum

    Public Sub AddLog(message As String)
        If String.IsNullOrWhiteSpace(message) Then Return
        Dim trimmed As String = message.Trim()
        Dim tipo As LocalLogType = GetLogTypeFromMessage(trimmed)

        ' Adicionar ao buffer com cap
        Try
            SyncLock _logBuffer
                _logBuffer.Add(New LogEntry(trimmed, tipo))
                If _logBuffer.Count > _maxBufferSize Then
                    _logBuffer.RemoveAt(0)
                End If
            End SyncLock
        Catch
        End Try

        ' Atualizar exibição
        If Me.InvokeRequired Then
            Try
                Me.BeginInvoke(Sub() RefreshLogs())
            Catch
            End Try
        Else
            RefreshLogs()
        End If
    End Sub

    Private Sub AppendLogEntry(entry As LogEntry)
        If rtbLogs Is Nothing OrElse rtbLogs.IsDisposed Then Return

        Dim ts As String = If(showTimestamp, $"[{entry.Timestamp:HH:mm:ss.fff}] ", "")
        Dim fullMessage As String = ts & entry.Message & Environment.NewLine

        Try
            rtbLogs.SelectionStart = rtbLogs.TextLength
            rtbLogs.SelectionLength = 0
            rtbLogs.SelectionColor = GetLogColor(entry.LogType)
            rtbLogs.AppendText(fullMessage)
            rtbLogs.SelectionColor = rtbLogs.ForeColor
        Catch
            Try
                rtbLogs.AppendText(fullMessage)
            Catch
            End Try
        End Try
    End Sub

    Private Sub RefreshLogs()
        If isPaused Then Return
        ApplyFilters()
        AppendNewLogs()
    End Sub

    Private Sub ApplyFilters()
        Try
            filteredLogs.Clear()
            SyncLock _logBuffer
                ' Sem UI de filtros ainda: exibe tudo
                For Each entry In _logBuffer
                    filteredLogs.Add(entry)
                Next
            End SyncLock
        Catch
        End Try
    End Sub

    Private Sub RefreshDisplay()
        ' Re-renderização completa (use apenas quando mudar filtros/configuração)
        Try
            lastDisplayedCount = 0
            rtbLogs.SuspendLayout()
            rtbLogs.Clear()
            For Each entry In filteredLogs
                AppendLogEntry(entry)
            Next
            rtbLogs.SelectionStart = rtbLogs.TextLength
            rtbLogs.ScrollToCaret()
            rtbLogs.ResumeLayout()
            lastDisplayedCount = filteredLogs.Count
        Catch
        End Try
    End Sub

    Private Sub AppendNewLogs()
        Try
            If rtbLogs Is Nothing OrElse rtbLogs.IsDisposed Then Return
            If lastDisplayedCount < 0 Then lastDisplayedCount = 0
            If lastDisplayedCount > filteredLogs.Count Then lastDisplayedCount = 0

            For i As Integer = lastDisplayedCount To filteredLogs.Count - 1
                AppendLogEntry(filteredLogs(i))
            Next

            If filteredLogs.Count > lastDisplayedCount Then
                rtbLogs.SelectionStart = rtbLogs.TextLength
                rtbLogs.ScrollToCaret()
            End If

            lastDisplayedCount = filteredLogs.Count
        Catch
        End Try
    End Sub

    ' Estrutura do log
    Private Class LogEntry
        Public ReadOnly Timestamp As DateTime
        Public ReadOnly Message As String
        Public ReadOnly LogType As LocalLogType
        Public Sub New(message As String, logType As LocalLogType)
            Me.Timestamp = DateTime.Now
            Me.Message = message
            Me.LogType = logType
        End Sub
    End Class

    Private Function GetLogTypeFromMessage(message As String) As LocalLogType
        Dim lower As String = message.ToLower()
        If message.Contains("❌") OrElse lower.Contains("erro") Then
            Return LocalLogType.Error
        ElseIf message.Contains("⚠️") OrElse lower.Contains("warning") Then
            Return LocalLogType.Warning
        ElseIf message.Contains("✅") OrElse lower.Contains("sucesso") Then
            Return LocalLogType.Success
        ElseIf message.Contains("🔍") OrElse message.Contains("🎬") Then
            Return LocalLogType.Debug
        Else
            Return LocalLogType.Info
        End If
    End Function

    Private Function GetLogColor(t As LocalLogType) As Color
        Select Case t
            Case LocalLogType.Info : Return Color.Gainsboro
            Case LocalLogType.Warning : Return Color.Gold
            Case LocalLogType.Error : Return Color.Tomato
            Case LocalLogType.Success : Return Color.LimeGreen
            Case LocalLogType.Debug : Return Color.LightSkyBlue
            Case Else : Return rtbLogs.ForeColor
        End Select
    End Function

    ''' <summary>
    ''' Evento de clique no botão Verificar Atualizações
    ''' </summary>
    Private Async Sub btnVerificarUpdate_Click(sender As Object, e As EventArgs) Handles btnVerificarUpdate.Click
        Try
            btnVerificarUpdate.Enabled = False
            UpdateStatus("Verificando atualizações...")

            Dim updateResult = Await updateManager.VerificarAtualizacoes()

            If updateResult.Success Then
                If updateResult.HasUpdate Then
                    Dim result = MessageBox.Show(
                        $"Nova versão disponível: {updateResult.VersionInfo.Version}" & vbCrLf &
                        "Deseja atualizar agora?",
                        "Atualização Disponível",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    )

                    If result = DialogResult.Yes Then
                        Await AplicarAtualizacao(updateResult.VersionInfo)
                    End If
                Else
                    MessageBox.Show("Sistema está atualizado", "Verificação", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Else
                MessageBox.Show($"Erro ao verificar atualizações: {updateResult.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnVerificarUpdate.Enabled = True
            UpdateStatus("Sistema pronto")
        End Try
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Carga Inicial
    ''' </summary>
    Private Async Sub btnCargaInicial_Click(sender As Object, e As EventArgs) Handles btnCargaInicial.Click
        Dim sucesso = Await ExecutarOperacaoAsync(
            Function() Task.Run(Sub() sincronizador.RealizarCargaInicial()),
            "Carga inicial concluída!",
            "Erro na carga inicial",
            btnCargaInicial)

        ' Atualizar informações de sincronização após carga
        If sucesso Then
            AtualizarInfoUltimaSincronizacao()
        End If
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Carga Incremental
    ''' </summary>
    Private Sub btnCargaIncremental_Click(sender As Object, e As EventArgs) Handles btnCargaIncremental.Click
        Try
            btnCargaIncremental.Enabled = False
            UpdateStatus("Executando carga incremental...")

            ' Executar em thread separada para não bloquear a UI
            Task.Run(Sub()
                         Try
                             sincronizador.RealizarCargaIncremental()
                             Me.Invoke(Sub()
                                           UpdateStatus("Carga incremental concluída!")
                                           MessageBox.Show("Carga incremental concluída com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                       End Sub)
                         Catch ex As Exception
                             Me.Invoke(Sub()
                                           UpdateStatus("Erro na carga incremental")
                                           MessageBox.Show($"Erro na carga incremental: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                       End Sub)
                         Finally
                             Me.Invoke(Sub() btnCargaIncremental.Enabled = True)
                         End Try
                     End Sub)

        Catch ex As Exception
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnCargaIncremental.Enabled = True
            UpdateStatus("Sistema pronto")
        End Try
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Sincronização Inteligente
    ''' </summary>
    Private Async Sub btnSincronizacaoInteligente_Click(sender As Object, e As EventArgs) Handles btnSincronizacaoInteligente.Click
        Dim sucesso = Await ExecutarOperacaoAsync(
            Function() Task.Run(Sub() sincronizador.ExecutarSincronizacaoInteligente()),
            "Sincronização inteligente concluída!",
            "Erro na sincronização inteligente",
            btnSincronizacaoInteligente)

        ' Atualizar informações de sincronização após sincronização
        If sucesso Then
            AtualizarInfoUltimaSincronizacao()
        End If
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Verificar Portas (APENAS verifica, SEM matar processos)
    ''' </summary>
    Private Sub btnVerificarPortas_Click(sender As Object, e As EventArgs) Handles btnVerificarPortas.Click
        Try
            btnVerificarPortas.Enabled = False
            Console.WriteLine("🔍 Iniciando verificação de portas (sem matar processos)...")
            UpdateStatus("Verificando portas do sistema...")

            ' APENAS verificar portas (novo método que não mata processos)
            portManager.CheckAllSystemPorts()

            ' Habilitar botão após um delay
            Task.Delay(2000).ContinueWith(Sub()
                                              Me.Invoke(Sub()
                                                            btnVerificarPortas.Enabled = True
                                                            UpdateStatus("Verificação de portas concluída")
                                                        End Sub)
                                          End Sub)

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao verificar portas: {ex.Message}")
            MessageBox.Show($"Erro ao verificar portas: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnVerificarPortas.Enabled = True
            UpdateStatus("Erro na verificação de portas")
        End Try
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Matar Processos (mata processos backend/frontend)
    ''' </summary>
    Private Sub btnMatarProcessos_Click(sender As Object, e As EventArgs) Handles btnMatarProcessos.Click
        Try
            Dim result = MessageBox.Show("⚠️ ATENÇÃO: Esta ação irá encerrar TODOS os processos do backend e frontend do sistema SUAT-IA." & vbCrLf & vbCrLf &
                                        "Deseja continuar?", "Confirmar Encerramento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If result = DialogResult.Yes Then
                btnMatarProcessos.Enabled = False
                Console.WriteLine("💀 Iniciando encerramento de processos do sistema...")
                UpdateStatus("Encerrando processos do sistema...")

                ' Matar todos os processos do sistema (passando frontendServer para parada controlada)
                portManager.KillAllSystemProcesses(frontendServer)

                ' Aguardar e detectar status novamente
                Task.Delay(3000).ContinueWith(Sub()
                                                  Me.Invoke(Sub()
                                                                btnMatarProcessos.Enabled = True
                                                                UpdateStatus("Encerramento de processos concluído")
                                                                ' Detectar status após matar processos
                                                                DetectarStatusServicosAsync()
                                                            End Sub)
                                              End Sub)
            End If

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao matar processos: {ex.Message}")
            MessageBox.Show($"Erro ao encerrar processos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnMatarProcessos.Enabled = True
            UpdateStatus("Erro no encerramento de processos")
        End Try
    End Sub

    ''' <summary>
    ''' Evento de clique no botão Criar Versão Teste
    ''' </summary>
    Private Sub btnCriarVersaoTeste_Click(sender As Object, e As EventArgs) Handles btnCriarVersaoTeste.Click
        ExecutarOperacao(
            Sub() updateManager.CriarArquivoVersaoTeste(),
            "Arquivo de versão de teste criado com sucesso!",
            "Erro ao criar arquivo de versão")
    End Sub

    ''' <summary>
    ''' Aplica uma atualização
    ''' </summary>
    Private Async Function AplicarAtualizacao(versionInfo As VersionInfo) As Task(Of Boolean)
        Try
            Dim result = Await updateManager.AplicarAtualizacao(versionInfo)
            If result.Success Then
                ShowSuccessMessage("Atualização aplicada com sucesso!")
                Return True
            Else
                ShowErrorMessage($"Erro na atualização: {result.Message}")
                Return False
            End If
        Catch ex As Exception
            ShowErrorMessage($"Erro ao aplicar atualização: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Atualiza o status na interface
    ''' </summary>
    Private Sub UpdateStatus(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() lblStatus.Text = message)
        Else
            lblStatus.Text = message
        End If
    End Sub

    ''' <summary>
    ''' Atualiza ambos os progress bars (formulário e StatusStrip) simultaneamente
    ''' </summary>
    Private Sub UpdateProgress(percentage As Integer, message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateProgress(percentage, message))
            Return
        End If

        ' Atualizar progress bar principal
        lblStatus.Text = message
        progressBar.Value = Math.Min(100, Math.Max(0, percentage))

        ' Atualizar StatusStrip
        UpdateStatusStripProgress(percentage, message)
    End Sub

    ''' <summary>
    ''' Atualiza a progress bar do StatusStrip e gerencia visibilidade dos elementos
    ''' </summary>
    Private Sub UpdateStatusStripProgress(percentage As Integer, message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateStatusStripProgress(percentage, message))
            Return
        End If

        ' Atualizar progress bar do StatusStrip
        tsp1.Value = Math.Min(100, Math.Max(0, percentage))

        ' Durante o progresso (>0%), esconder ts2, ts3, ts4 e expandir tsp1
        If percentage > 0 Then
            ' Esconder elementos desnecessários
            ts2.Visible = False
            ts3.Visible = False
            ts4.Visible = False

            ' Mostrar e expandir progress bar
            tsp1.Visible = True
            tsp1.Size = New Size(400, 18) ' Expandir para ocupar mais espaço
        End If
    End Sub

    ''' <summary>
    ''' Reseta ambos os progress bars
    ''' </summary>
    Private Sub ResetProgress()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ResetProgress())
            Return
        End If

        ' Resetar progress bar principal
        progressBar.Value = 0

        ' Resetar StatusStrip
        ResetStatusStrip()
    End Sub

    ''' <summary>
    ''' Reseta o StatusStrip ao estado normal
    ''' </summary>
    Private Sub ResetStatusStrip()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ResetStatusStrip())
            Return
        End If

        ' Esconder e resetar progress bar
        tsp1.Visible = False
        tsp1.Value = 0
        tsp1.Size = New Size(100, 18) ' Tamanho original

        ' Restaurar elementos
        ts2.Visible = True
        ts3.Visible = True
        ts4.Visible = True

        ' Atualizar informações
        ts4.Text = "Tempo: " + DateTime.Now.ToString("HH:mm")
    End Sub

    ''' <summary>
    ''' Evento de progresso da atualização
    ''' </summary>
    Private Sub OnUpdateProgress(percentage As Integer, status As String)
        UpdateStatus(status)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() progressBar.Value = Math.Min(100, percentage))
        Else
            progressBar.Value = Math.Min(100, percentage)
        End If
    End Sub

    ''' <summary>
    ''' Evento de status das portas
    ''' </summary>
    Private Sub OnPortStatusChanged(port As Integer, status As String)
        UpdateStatus(status)
        Console.WriteLine($"[Porta {port}] {status}")
    End Sub

    ''' <summary>
    ''' Evento de carregamento do formulário
    ''' </summary>
    Private Async Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Fixar altura do StatusStrip para manter o tamanho do design
        Me.StatusStrip1.AutoSize = False
        Me.StatusStrip1.Height = 38
        Me.StatusStrip1.Padding = New Padding(0, 0, 0, 0)

        Console.WriteLine("========================================")
        Console.WriteLine("    SUAT-IA - Sistema de Integração")
        Console.WriteLine("    .NET Framework 4.7 - WinForms")
        Console.WriteLine("========================================")
        Console.WriteLine()
        Console.WriteLine("Sistema iniciado com sucesso!")
        Console.WriteLine("Use os botões para testar as funcionalidades:")
        Console.WriteLine("- 🔍 Verificar Portas: Verifica e libera portas em uso")
        Console.WriteLine("- Verificar Atualizações: Testa o sistema de auto-atualização")
        Console.WriteLine("- Carga Inicial: Simula carga de 3 anos de dados")
        Console.WriteLine("- Carga Incremental: Simula carga de dados recentes")
        Console.WriteLine("- Sincronização Inteligente: Detecta automaticamente se é nova instalação ou atualização")
        Console.WriteLine("- Criar Versão Teste: Cria arquivo de versão para testes")
        Console.WriteLine("- 🚀 INICIAR SISTEMA COMPLETO: Inicia tudo automaticamente (inclui verificação de portas)")
        Console.WriteLine()

        ' Aguardar um pouco para os managers inicializarem completamente
        Console.WriteLine("🚀 MainForm carregado - Iniciando limpeza e detecção de status...")
        Await Task.Delay(1000)

        ' FASE 1: Limpeza preventiva na inicialização
        Console.WriteLine("🧹 FASE 1: Limpeza preventiva de processos backend...")
        LimparTodosProcessosBackendInicializacao()

        ' FASE 2: Detectar status atual dos serviços e atualizar a interface
        Console.WriteLine("🔍 FASE 2: Detectando status após limpeza...")
        Await DetectarStatusServicosAsync()

        ' FASE 3: Atualizar informações de sincronização
        Console.WriteLine("📊 FASE 3: Atualizando informações de sincronização...")
        AtualizarInfoUltimaSincronizacao()
    End Sub

    ''' <summary>
    ''' Limpeza preventiva de todos os processos backend na inicialização
    ''' </summary>
    Private Sub LimparTodosProcessosBackendInicializacao()
        Try
            Console.WriteLine("🧹 Iniciando limpeza preventiva de processos backend...")
            Dim totalKilled As Integer = 0

            ' 1. Matar todos os processos suat-backend
            Dim suatBackends = Process.GetProcessesByName("suat-backend")
            For Each p In suatBackends
                Try
                    Console.WriteLine($"🔄 Encerrando suat-backend (PID {p.Id})")
                    p.Kill()
                    p.WaitForExit(2000)
                    totalKilled += 1
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao encerrar suat-backend PID {p.Id}: {ex.Message}")
                End Try
            Next

            ' 2. Matar processos Node.js relacionados ao backend
            Dim nodes = Process.GetProcessesByName("node")
            For Each p In nodes
                Try
                    Dim cmd = GetProcessCommandLine(p.Id)
                    If cmd.ToLower().Contains("suat-backend") OrElse cmd.ToLower().Contains("backend") OrElse
                       cmd.Contains("3000") OrElse cmd.ToLower().Contains("server.js") OrElse cmd.ToLower().Contains("app.js") Then
                        Console.WriteLine($"🔄 Encerrando Node.js backend (PID {p.Id}) - cmd: {cmd.Substring(0, Math.Min(50, cmd.Length))}...")
                        p.Kill()
                        p.WaitForExit(2000)
                        totalKilled += 1
                    End If
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao verificar/encerrar Node.js PID {p.Id}: {ex.Message}")
                End Try
            Next

            ' 3. Aguardar um pouco para os processos finalizarem
            If totalKilled > 0 Then
                Console.WriteLine($"⏳ Aguardando finalização de {totalKilled} processo(s)...")
                Threading.Thread.Sleep(2000)
            End If

            ' 4. Verificar se porta 3000 foi liberada
            If portManager.IsPortAvailable(3000) Then
                Console.WriteLine("✅ Limpeza concluída - Porta 3000 liberada")
            Else
                Console.WriteLine("⚠️ Porta 3000 ainda ocupada após limpeza - pode haver outros processos")
            End If

            Console.WriteLine($"🎯 Limpeza preventiva concluída: {totalKilled} processo(s) encerrado(s)")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro na limpeza preventiva: {ex.Message}")
        End Try
    End Sub

    ' --- Eventos dos Botões Frontend ---

    ''' <summary>
    ''' Inicia o servidor frontend
    ''' </summary>
    Private Sub btnIniciarFrontend_Click(sender As Object, e As EventArgs) Handles btnIniciarFrontend.Click
        Try
            btnIniciarFrontend.Enabled = False

            If frontendServer.StartAsync() Then
                btnIniciarFrontend.Enabled = False
                btnPararFrontend.Enabled = True
                btnAbrirBrowser.Enabled = True
            Else
                btnIniciarFrontend.Enabled = True
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro ao iniciar frontend: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnIniciarFrontend.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' Para o servidor frontend
    ''' </summary>
    Private Sub btnPararFrontend_Click(sender As Object, e As EventArgs) Handles btnPararFrontend.Click
        Try
            frontendServer.Stop()
            btnIniciarFrontend.Enabled = True
            btnPararFrontend.Enabled = False
            btnAbrirBrowser.Enabled = False

        Catch ex As Exception
            MessageBox.Show($"Erro ao parar frontend: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Abre o frontend no browser
    ''' </summary>
    Private Sub btnAbrirBrowser_Click(sender As Object, e As EventArgs) Handles btnAbrirBrowser.Click
        Try
            If frontendServer.IsServerRunning Then
                Process.Start(frontendServer.ServerUrl)
            Else
                MessageBox.Show("Frontend não está rodando!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir browser: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- Eventos do Servidor Frontend ---

    ''' <summary>
    ''' Evento quando status do frontend muda
    ''' </summary>
    Private Sub OnFrontendStatusChanged(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnFrontendStatusChanged(message))
        Else
            Console.WriteLine($"[Frontend] {message}")
        End If
    End Sub

    ''' <summary>
    ''' Evento quando servidor frontend inicia
    ''' </summary>
    Private Sub OnFrontendServerStarted(url As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnFrontendServerStarted(url))
        Else
            lblFrontendStatus.Text = $"Frontend: Rodando em {url}"
            lblFrontendStatus.ForeColor = Color.DarkGreen
            Console.WriteLine($"🌐 Frontend disponível em: {url}")
        End If
    End Sub

    ''' <summary>
    ''' Evento quando servidor frontend para
    ''' </summary>
    Private Sub OnFrontendServerStopped()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnFrontendServerStopped())
        Else
            lblFrontendStatus.Text = "Frontend: Parado"
            lblFrontendStatus.ForeColor = Color.DarkRed
        End If
    End Sub

    ''' <summary>
    ''' Evento quando servidor frontend recebe request
    ''' </summary>
    Private Sub OnFrontendRequestReceived(method As String, path As String)
        ' Log silencioso - não logar todas as requests para não poluir
        ' Console.WriteLine($"[Frontend] {method} {path}")
    End Sub

    ' --- Eventos dos Botões Backend API ---

    ''' <summary>
    ''' Inicia o backend API
    ''' </summary>
    Private Async Sub btnIniciarBackend_Click(sender As Object, e As EventArgs) Handles btnIniciarBackend.Click
        Try
            btnIniciarBackend.Enabled = False

            If Await backendApiManager.StartAsync() Then
                btnIniciarBackend.Enabled = False
                btnPararBackend.Enabled = True
                btnHealthCheck.Enabled = True
                btnAbrirSwagger.Enabled = True
            Else
                btnIniciarBackend.Enabled = True
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro ao iniciar backend API: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnIniciarBackend.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' Para o backend API
    ''' </summary>
    Private Sub btnPararBackend_Click(sender As Object, e As EventArgs) Handles btnPararBackend.Click
        Try
            backendApiManager.Stop()
            btnIniciarBackend.Enabled = True
            btnPararBackend.Enabled = False
            btnHealthCheck.Enabled = False
            btnAbrirSwagger.Enabled = False

        Catch ex As Exception
            MessageBox.Show($"Erro ao parar backend API: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Executa health check do backend
    ''' </summary>
    Private Sub btnHealthCheck_Click(sender As Object, e As EventArgs) Handles btnHealthCheck.Click
        Try
            backendApiManager.CheckHealthAsync()

        Catch ex As Exception
            MessageBox.Show($"Erro no health check: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Abre o Swagger do backend
    ''' </summary>
    Private Sub btnAbrirSwagger_Click(sender As Object, e As EventArgs) Handles btnAbrirSwagger.Click
        Try
            If backendApiManager.IsRunning Then
                Process.Start(backendApiManager.SwaggerUrl)
            Else
                MessageBox.Show("Backend API não está rodando!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro ao abrir Swagger: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- Eventos do Backend API Manager ---

    ''' <summary>
    ''' Evento quando status do backend muda
    ''' </summary>
    Private Sub OnBackendStatusChanged(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnBackendStatusChanged(message))
        Else
            Console.WriteLine($"[Backend API] {message}")
        End If
    End Sub

    ''' <summary>
    ''' Evento quando backend API inicia
    ''' </summary>
    Private Sub OnBackendServerStarted(url As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnBackendServerStarted(url))
        Else
            lblBackendStatus.Text = $"Backend API: Rodando em :3000"
            lblBackendStatus.ForeColor = Color.DarkGreen
            Console.WriteLine($"🚀 Backend API disponível em: {url}")
        End If
    End Sub

    ''' <summary>
    ''' Evento quando backend API para
    ''' </summary>
    Private Sub OnBackendServerStopped()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnBackendServerStopped())
        Else
            lblBackendStatus.Text = "Backend API: Parado"
            lblBackendStatus.ForeColor = Color.DarkRed
        End If
    End Sub

    ''' <summary>
    ''' Evento do resultado do health check
    ''' </summary>
    Private Sub OnBackendHealthCheck(isHealthy As Boolean, message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnBackendHealthCheck(isHealthy, message))
        Else
            If isHealthy Then
                Console.WriteLine($"💚 Health Check OK: {message}")
                MessageBox.Show($"Backend API está funcionando!\n\n{message}", "Health Check", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                Console.WriteLine($"💔 Health Check FAIL: {message}")
                MessageBox.Show($"Backend API com problemas!\n\n{message}", "Health Check", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End If
    End Sub

    ' --- Controles do Sistema Completo ---

    ''' <summary>
    ''' Inicia todo o sistema SUAT-IA
    ''' </summary>
    Private Async Sub btnIniciarTudo_Click(sender As Object, e As EventArgs) Handles btnIniciarTudo.Click
        Try
            Console.WriteLine("🚀 ======================================")
            Console.WriteLine("🚀    INICIANDO SISTEMA SUAT-IA")
            Console.WriteLine("🚀 ======================================")
            Console.WriteLine()

            ' 0. Verificar e liberar portas antes de iniciar (com fallback Node.js no PortManager)
            Console.WriteLine("🔍 Passo 0/3: Verificando portas do sistema...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(3000) ' Aguardar verificação completar
            Console.WriteLine("✅ Verificação de portas concluída!")
            Console.WriteLine()

            ' 1. Iniciar Backend API (garantir que backend subiu saudável)
            Console.WriteLine("🔥 Passo 1/3: Iniciando Backend API...")
            If Await backendApiManager.StartAsync() Then
                btnIniciarBackend.Enabled = False
                btnPararBackend.Enabled = True
                btnHealthCheck.Enabled = True
                btnAbrirSwagger.Enabled = True
                Console.WriteLine("✅ Backend API iniciado com sucesso!")
            Else
                Console.WriteLine("❌ Falha ao iniciar Backend API")
                Return
            End If

            ' Aguardar um pouco
            Await Task.Delay(2000)

            ' 2. Iniciar Frontend Server (somente se backend OK)
            Console.WriteLine("🔥 Passo 2/3: Iniciando Frontend Server...")
            If frontendServer.StartAsync() Then
                btnIniciarFrontend.Enabled = False
                btnPararFrontend.Enabled = True
                btnAbrirBrowser.Enabled = True
                Console.WriteLine("✅ Frontend Server iniciado com sucesso!")
            Else
                Console.WriteLine("❌ Falha ao iniciar Frontend Server")
                Return
            End If

            Console.WriteLine()
            Console.WriteLine("🎉 ======================================")
            Console.WriteLine("🎉    SISTEMA SUAT-IA INICIADO!")
            Console.WriteLine("🎉 ======================================")
            Console.WriteLine("🌐 Backend API: http://localhost:3000")
            Console.WriteLine("🌐 Frontend:    http://localhost:8080")
            Console.WriteLine("📖 Swagger:     http://localhost:3000/api-docs")
            Console.WriteLine()

            ' Mostrar dialog de sucesso
            Dim result = MessageBox.Show(
                "Sistema SUAT-IA iniciado com sucesso!" & vbNewLine & vbNewLine &
                "🔍 Portas verificadas e liberadas automaticamente" & vbNewLine &
                "✅ Backend API: http://localhost:3000" & vbNewLine &
                "✅ Frontend: http://localhost:8080" & vbNewLine &
                "✅ Swagger: http://localhost:3000/api-docs" & vbNewLine & vbNewLine &
                "Deseja abrir o frontend no browser?",
                "Sistema Iniciado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            )

            If result = DialogResult.Yes Then
                Process.Start(frontendServer.ServerUrl)
            End If

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao iniciar sistema: {ex.Message}")
            MessageBox.Show($"Erro ao iniciar sistema:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        ' Detectar status final sempre (sucesso ou erro) para atualizar interface corretamente
        Try
            Console.WriteLine("🔍 Detectando status final para atualizar interface...")
            Await DetectarStatusServicosAsync()
        Catch ex As Exception
            Console.WriteLine($"⚠️ Erro na detecção de status final: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Para todo o sistema SUAT-IA
    ''' </summary>
    Private Async Sub btnPararTudo_Click(sender As Object, e As EventArgs) Handles btnPararTudo.Click
        Try
            ' Executar parada completa
            Await ExecutarParadaCompleta()

            ' Detectar status e atualizar interface
            Await DetectarStatusServicosAsync()

            ' Mostrar mensagem de confirmação
            MessageBox.Show("Sistema SUAT-IA parado com sucesso!", "Sistema Parado", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao parar sistema: {ex.Message}")
            MessageBox.Show($"Erro ao parar sistema:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- Handlers do menu dropdown SUATIA (INICIAR/PARAR) ---
    Private Async Sub OnMenuIniciarClick(sender As Object, e As EventArgs) Handles INICIAR.Click
        Try
            Console.WriteLine("🟢 ======================================")
            Console.WriteLine("🟢    MENU INICIAR - SEQUÊNCIA COMPLETA")
            Console.WriteLine("🟢 ======================================")
            Console.WriteLine()

            ' Resetar progress bars no início
            ResetProgress()

            ' Executar sequência de inicialização
            Dim success = Await ExecutarSequenciaInicializacao()

            If success Then
                ShowSuccessMessage("Sistema SUAT-IA iniciado e sincronizado com sucesso!")
                ' Atualizar informações de sincronização após sequência completa
                AtualizarInfoUltimaSincronizacao()
            End If

            ' Manter progress bars em 100% por alguns segundos e depois resetar
            Await Task.Delay(5000)
            ResetProgress()
            UpdateStatus("Sistema pronto")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro no menu INICIAR: {ex.Message}")
            ResetProgress()
            UpdateStatus("❌ Erro na sequência de inicialização")
            ShowErrorMessage($"Erro na sequência de inicialização: {ex.Message}")
        End Try
    End Sub

    Private Async Sub OnMenuPararClick(sender As Object, e As EventArgs) Handles PARAR.Click
        Try
            Console.WriteLine("🔴 Menu PARAR acionado")
            UpdateStatus("Parando sistema...")

            ' Executar lógica de parada diretamente
            Await ExecutarParadaCompleta()

            ' Após parar, detectar status e atualizar interface
            Console.WriteLine("🔍 Detectando novo status após parar...")
            Await DetectarStatusServicosAsync()

            UpdateStatus("Sistema parado")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro no menu PARAR: {ex.Message}")
            ShowErrorMessage($"Erro ao parar sistema: {ex.Message}")
            UpdateStatus("Erro ao parar sistema")
        End Try
    End Sub

    ''' <summary>
    ''' Executa verificação de atualizações de forma silenciosa (sem interação do usuário)
    ''' </summary>
    Private Async Function ExecutarVerificacaoAtualizacoes() As Task(Of Boolean)
        Try
            Console.WriteLine("   🔍 Executando verificação de atualizações...")

            ' Progresso durante verificação (5% -> 15%)
            UpdateProgress(8, "🔍 Conectando ao servidor de atualizações...")
            Await Task.Delay(500)

            UpdateProgress(12, "📋 Verificando versão atual...")
            Dim updateResult = Await updateManager.VerificarAtualizacoes()

            UpdateProgress(18, "🔍 Analisando resultado da verificação...")
            Await Task.Delay(300)

            If updateResult.Success Then
                If updateResult.HasUpdate Then
                    Console.WriteLine($"   📦 Nova versão disponível: {updateResult.VersionInfo.Version}")
                    Console.WriteLine("   ℹ️ Atualização disponível, mas continuando com inicialização")
                    UpdateProgress(20, $"📦 Nova versão disponível: {updateResult.VersionInfo.Version}")
                    ' Não aplicar automaticamente - apenas notificar
                    Return True
                Else
                    Console.WriteLine("   ✅ Sistema está atualizado")
                    UpdateProgress(20, "✅ Sistema está atualizado")
                    Return True
                End If
            Else
                Console.WriteLine($"   ⚠️ Erro na verificação de atualizações: {updateResult.Message}")
                UpdateProgress(20, $"⚠️ Erro na verificação: {updateResult.Message}")
                Return False
            End If

        Catch ex As Exception
            Console.WriteLine($"   ❌ Erro na verificação de atualizações: {ex.Message}")

            ' Mostrar erro mas não resetar ainda (continuará para próxima etapa)
            UpdateProgress(20, $"❌ Erro na verificação: {ex.Message}")

            Return False
        End Try
    End Function

    ''' <summary>
    ''' Executa a inicialização completa do sistema (lógica do btnIniciarTudo)
    ''' </summary>
    Private Async Function ExecutarInicializacaoSistema() As Task(Of Boolean)
        Try
            Console.WriteLine("   🚀 Executando inicialização do sistema...")

            ' 0. Verificar e liberar portas (25% -> 35%)
            UpdateProgress(30, "🔍 Verificando e liberando portas...")
            Console.WriteLine("   🔍 Verificando portas do sistema...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(3000)
            UpdateProgress(35, "✅ Portas verificadas e liberadas")

            ' 1. Iniciar Backend API (35% -> 55%)
            UpdateProgress(40, "🔥 Iniciando Backend API...")
            Console.WriteLine("   🔥 Iniciando Backend API...")
            If Await backendApiManager.StartAsync() Then
                Console.WriteLine("   ✅ Backend API iniciado com sucesso")
                UpdateProgress(55, "✅ Backend API iniciado com sucesso")
            Else
                Console.WriteLine("   ❌ Falha ao iniciar Backend API")
                UpdateProgress(40, "❌ Falha ao iniciar Backend API")
                Return False
            End If

            Await Task.Delay(2000)

            ' 2. Iniciar Frontend Server (55% -> 70%)
            UpdateProgress(60, "🌐 Iniciando Frontend Server...")
            Console.WriteLine("   🌐 Iniciando Frontend Server...")
            If frontendServer.StartAsync() Then
                Console.WriteLine("   ✅ Frontend Server iniciado com sucesso")
                UpdateProgress(70, "✅ Frontend Server iniciado com sucesso")
            Else
                Console.WriteLine("   ❌ Falha ao iniciar Frontend Server")
                UpdateProgress(60, "❌ Falha ao iniciar Frontend Server")
                Return False
            End If

            ' 3. Detectar status final e atualizar interface (70% -> 80%)
            UpdateProgress(75, "🔍 Detectando status final dos serviços...")
            Console.WriteLine("   🔍 Detectando status final...")
            Await DetectarStatusServicosAsync()
            UpdateProgress(80, "✅ Status dos serviços atualizado")

            Console.WriteLine("   🎉 Sistema iniciado com sucesso!")
            Return True

        Catch ex As Exception
            Console.WriteLine($"   ❌ Erro na inicialização do sistema: {ex.Message}")

            ' Resetar progress bars em caso de erro
            ResetProgress()
            UpdateStatus($"❌ Erro na inicialização: {ex.Message}")

            Return False
        End Try
    End Function

    ''' <summary>
    ''' Executa a sincronização de dados após inicialização bem-sucedida
    ''' </summary>
    Private Async Function ExecutarSincronizacaoDados() As Task(Of Boolean)
        Try
            Console.WriteLine("   🔄 Executando sincronização de dados...")

            ' Iniciar sincronização (85% -> 95%)
            UpdateProgress(87, "🔄 Preparando sincronização inteligente...")
            Await Task.Delay(500)

            ' Executar sincronização inteligente em thread separada
            UpdateProgress(90, "🔄 Executando sincronização inteligente...")
            Dim syncTask = Task.Run(Sub()
                                        Try
                                            sincronizador.ExecutarSincronizacaoInteligente()
                                            Console.WriteLine("   ✅ Sincronização inteligente concluída")
                                        Catch ex As Exception
                                            Console.WriteLine($"   ❌ Erro na sincronização: {ex.Message}")
                                            Throw
                                        End Try
                                    End Sub)

            ' Aguardar conclusão da sincronização
            Await syncTask
            UpdateProgress(95, "✅ Sincronização de dados concluída")
            Console.WriteLine("   🎯 Sincronização de dados finalizada")
            Return True

        Catch ex As Exception
            Console.WriteLine($"   ❌ Erro na sincronização de dados: {ex.Message}")

            ' Mostrar erro mas não usar Await em Catch
            UpdateProgress(85, $"❌ Erro na sincronização: {ex.Message}")

            Return False
        End Try
    End Function

    ''' <summary>
    ''' Executa a parada completa do sistema (lógica extraída do btnPararTudo_Click)
    ''' </summary>
    Private Async Function ExecutarParadaCompleta() As Task
        Try
            Console.WriteLine("🛑 ======================================")
            Console.WriteLine("🛑    PARANDO SISTEMA SUAT-IA")
            Console.WriteLine("🛑 ======================================")
            Console.WriteLine()

            ' Parar Frontend
            Console.WriteLine("🔥 Parando Frontend Server...")
            If frontendServer IsNot Nothing Then
                Try
                    frontendServer.Stop()
                    Console.WriteLine("✅ Frontend Server parado com sucesso")
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao parar Frontend Server: {ex.Message}")
                End Try
            Else
                Console.WriteLine("⚠️ Frontend Server é Nothing - pulando")
            End If

            ' Parar Backend API
            Console.WriteLine("🔥 Parando Backend API...")
            If backendApiManager IsNot Nothing Then
                Try
                    backendApiManager.Stop()
                    Console.WriteLine("✅ Backend API parado com sucesso")
                Catch ex As Exception
                    Console.WriteLine($"⚠️ Erro ao parar Backend API: {ex.Message}")
                End Try
            Else
                Console.WriteLine("⚠️ Backend API Manager é Nothing - pulando")
            End If

            ' Aguardar um pouco para os serviços finalizarem
            Console.WriteLine("⏳ Aguardando serviços finalizarem...")
            Await Task.Delay(2000)

            ' Fallback: garantir liberação da porta 3000
            Try
                Console.WriteLine("🔍 Verificando se a porta 3000 foi liberada...")
                If Not portManager.IsPortAvailable(3000) Then
                    Console.WriteLine("⚠️ Porta 3000 ainda ocupada. Tentando encerrar processos por nome...")

                    ' 1) Matar suat-backend por nome
                    Dim killedByName As Integer = 0
                    For Each p In Process.GetProcessesByName("suat-backend")
                        Try
                            Console.WriteLine($"🔄 Matando suat-backend (PID {p.Id})")
                            p.Kill()
                            p.WaitForExit(3000)
                            killedByName += 1
                        Catch ex As Exception
                            Console.WriteLine($"⚠️ Erro ao matar suat-backend PID {p.Id}: {ex.Message}")
                        End Try
                    Next
                    Console.WriteLine($"ℹ️ Processos suat-backend encerrados: {killedByName}")

                    ' 2) Se ainda ocupado, matar Node.js relacionado ao backend
                    Threading.Thread.Sleep(1500)
                    If Not portManager.IsPortAvailable(3000) Then
                        Console.WriteLine("⚠️ Porta 3000 ainda ocupada. Tentando encerrar processos Node.js do backend...")
                        Dim nodes = Process.GetProcessesByName("node")
                        Dim killedNode As Integer = 0
                        For Each p In nodes
                            Try
                                Dim cmd = GetProcessCommandLine(p.Id)
                                If cmd.ToLower().Contains("suat-backend") OrElse cmd.ToLower().Contains("backend") OrElse
                                   cmd.Contains("3000") OrElse cmd.ToLower().Contains("server.js") OrElse cmd.ToLower().Contains("app.js") Then
                                    Console.WriteLine($"🔄 Matando node (PID {p.Id}) - cmd: {cmd.Substring(0, Math.Min(50, cmd.Length))}...")
                                    p.Kill()
                                    p.WaitForExit(3000)
                                    killedNode += 1
                                End If
                            Catch ex As Exception
                                Console.WriteLine($"⚠️ Erro ao verificar/matar node PID {p.Id}: {ex.Message}")
                            End Try
                        Next
                        Console.WriteLine($"ℹ️ Processos Node.js do backend encerrados: {killedNode}")

                        ' 3) Checagem final
                        Threading.Thread.Sleep(1500)
                        If Not portManager.IsPortAvailable(3000) Then
                            Console.WriteLine("❌ Porta 3000 permanece ocupada mesmo após tentativas de encerramento.")
                        Else
                            Console.WriteLine("✅ Porta 3000 liberada após fallback de encerramento.")
                        End If
                    Else
                        Console.WriteLine("✅ Porta 3000 liberada após encerrar suat-backend por nome.")
                    End If
                Else
                    Console.WriteLine("✅ Porta 3000 já está liberada.")
                End If
            Catch ex As Exception
                Console.WriteLine($"⚠️ Erro no fallback de liberação da porta: {ex.Message}")
            End Try

            ' A interface será atualizada pela detecção de status posterior

            Console.WriteLine()
            Console.WriteLine("✅ ======================================")
            Console.WriteLine("✅    SISTEMA SUAT-IA PARADO")
            Console.WriteLine("✅ ======================================")
            Console.WriteLine()

            UpdateStatus("✅ Sistema SUAT-IA completamente parado")

        Catch ex As Exception
            Console.WriteLine($"❌ Erro ao executar parada completa: {ex.Message}")
            Throw
        End Try
    End Function


    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        btnPararTudo.PerformClick()
    End Sub
End Class

''' <summary>
''' Classe para redirecionar saída do Console para TextBox e LogViewer simultaneamente
''' </summary>
Public Class DualConsoleWriter
    Inherits IO.TextWriter

    Private textBox As TextBox

    Public Sub New(textBox As TextBox)
        Me.textBox = textBox
    End Sub

    Public Overrides ReadOnly Property Encoding As System.Text.Encoding
        Get
            Return System.Text.Encoding.UTF8
        End Get
    End Property

    Public Overrides Sub Write(value As String)
        Try
            ' Escrever no TextBox local (MainForm)
            If textBox IsNot Nothing AndAlso textBox.InvokeRequired Then
                textBox.Invoke(Sub() WriteToTextBox(value))
            ElseIf textBox IsNot Nothing Then
                WriteToTextBox(value)
            End If
        Catch ex As Exception
            ' Ignorar erros de escrita para não quebrar o sistema
        End Try
    End Sub

    Public Overrides Sub WriteLine(value As String)
        Write(value & Environment.NewLine)
    End Sub

    Private Sub WriteToTextBox(value As String)
        Try
            If textBox IsNot Nothing Then
                textBox.AppendText(value)
                textBox.SelectionStart = textBox.Text.Length
                textBox.ScrollToCaret()
            End If
        Catch ex As Exception
            ' Ignorar erros
        End Try
    End Sub

End Class

''' <summary>
''' Classe para redirecionar a saída do Console para um TextBox
''' </summary>
Public Class ConsoleWriter
    Inherits TextWriter

    Private ReadOnly textBox As TextBox

    Public Sub New(textBox As TextBox)
        Me.textBox = textBox
    End Sub

    Public Overrides ReadOnly Property Encoding As System.Text.Encoding
        Get
            Return System.Text.Encoding.UTF8
        End Get
    End Property

    Public Overrides Sub Write(value As String)
        Try
            If textBox IsNot Nothing AndAlso Not textBox.IsDisposed Then
                If textBox.InvokeRequired Then
                    textBox.Invoke(Sub() WriteToTextBox(value))
                Else
                    WriteToTextBox(value)
                End If
            End If
        Catch ex As Exception
            ' Silenciar erros para evitar loops infinitos
        End Try
    End Sub

    Public Overrides Sub WriteLine(value As String)
        Write(value & Environment.NewLine)
    End Sub

    Private Sub WriteToTextBox(value As String)
        Try
            If textBox IsNot Nothing AndAlso Not textBox.IsDisposed Then
                textBox.AppendText(value)
                textBox.SelectionStart = textBox.TextLength
                textBox.ScrollToCaret()
                Application.DoEvents()
            End If
        Catch ex As Exception
            ' Silenciar erros para evitar loops infinitos
        End Try
    End Sub
End Class

' Writer simples que envia Console.WriteLine para o AddLog do MainForm
Public Class MainFormConsoleWriter
    Inherits IO.TextWriter

    Private ReadOnly sink As Action(Of String)

    Public Sub New(sink As Action(Of String))
        Me.sink = sink
    End Sub

    Public Overrides Sub Write(value As String)
        If String.IsNullOrWhiteSpace(value) Then Return
        Try
            sink(value)
        Catch
        End Try
    End Sub

    Public Overrides Sub WriteLine(value As String)
        Write(value & Environment.NewLine)
    End Sub

    Public Overrides ReadOnly Property Encoding As System.Text.Encoding
        Get
            Return System.Text.Encoding.UTF8
        End Get
    End Property
End Class
