Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading.Tasks
Imports System.Text

''' <summary>
''' Nova interface moderna e interativa para orquestração do SUAT-IA.
''' Mantém as funcionalidades principais com organização e estética atual.
''' </summary>
Public Class NovaInterfaceForm
    Inherits Form

    ' --- Gerenciadores ---
    Private ReadOnly updateManager As UpdateManager
    Private ReadOnly portManager As PortManager
    Private ReadOnly backendApiManager As BackendApiManager
    Private ReadOnly frontendServer As FrontendHttpServer
    Private ReadOnly sincronizador As SincronizadorDados
    Private ReadOnly databaseManager As DatabaseManager

    ' --- UI ---
    Private headerPanel As Panel
    Private lblTitulo As Label
    Private lblSubTitulo As Label
    Private lblBackendBadge As Label
    Private lblFrontendBadge As Label
    Private lblDbBadge As Label

    Private grpAcoesRapidas As GroupBox
    Private WithEvents btnIniciarTudo As Button
    Private WithEvents btnPararTudo As Button
    Private WithEvents btnAbrirFrontend As Button
    Private WithEvents btnAbrirSwagger As Button

    Private grpSincronizacao As GroupBox
    Private WithEvents btnCargaInicial As Button
    Private WithEvents btnCargaIncremental As Button
    Private WithEvents btnSyncInteligente As Button

    Private grpAtualizacoes As GroupBox
    Private WithEvents btnVerificarAtualizacoes As Button
    Private WithEvents btnCriarVersaoTeste As Button

    Private grpPortas As GroupBox
    Private WithEvents btnVerificarPortas As Button

    Private grpBanco As GroupBox
    Private WithEvents btnVerificarBanco As Button
    Private txtBancoInfo As TextBox

    Private txtLog As TextBox
    Private lblStatusOp As Label
    Private progressBar As ProgressBar
    Private progressTimer As Timer
    Private progressActive As Boolean = False

    ' Console redirection
    Private previousConsoleOut As TextWriter
    Private consoleWriter As ConsoleWriter

    Public Sub New()
        ' Inicializar gerenciadores isolados desta interface
        Me.updateManager = New UpdateManager()
        Me.portManager = New PortManager()
        Me.backendApiManager = New BackendApiManager()
        Dim frontendBuildPath = Path.Combine(Application.StartupPath, "frontend", "build")
        Me.frontendServer = New FrontendHttpServer(frontendBuildPath, 8080)
        Me.sincronizador = New SincronizadorDados()
        Me.databaseManager = New DatabaseManager()

        ' Eventos
        AddHandler updateManager.ProgressChanged, AddressOf OnUpdateProgress
        AddHandler portManager.PortStatusChanged, AddressOf OnPortStatusChanged
        AddHandler backendApiManager.StatusChanged, AddressOf OnBackendStatusChanged
        AddHandler backendApiManager.ServerStarted, AddressOf OnBackendStarted
        AddHandler backendApiManager.ServerStopped, AddressOf OnBackendStopped
        AddHandler backendApiManager.HealthCheckResult, AddressOf OnHealthCheck
        AddHandler frontendServer.StatusChanged, AddressOf OnFrontendStatusChanged
        AddHandler frontendServer.ServerStarted, AddressOf OnFrontendStarted
        AddHandler frontendServer.ServerStopped, AddressOf OnFrontendStopped

        InitializeComponent()
        AtualizarBadges()
    End Sub

    Private Sub InitializeComponent()
        ' Form
        Me.Text = "SUAT-IA - Nova Interface"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.ClientSize = New Size(980, 720)
        Me.BackColor = Color.FromArgb(248, 250, 252)

        ' Header
        headerPanel = New Panel()
        headerPanel.BackColor = Color.FromArgb(51, 65, 85)
        headerPanel.Size = New Size(Me.ClientSize.Width, 90)
        headerPanel.Location = New Point(0, 0)
        headerPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(headerPanel)

        lblTitulo = New Label()
        lblTitulo.Text = "SUAT-IA Orquestração"
        lblTitulo.Font = New Font("Segoe UI", 18.0!, FontStyle.Bold)
        lblTitulo.ForeColor = Color.White
        lblTitulo.Location = New Point(20, 15)
        lblTitulo.Size = New Size(500, 35)
        headerPanel.Controls.Add(lblTitulo)

        lblSubTitulo = New Label()
        lblSubTitulo.Text = "Interface moderna para controle completo do sistema"
        lblSubTitulo.Font = New Font("Segoe UI", 10.0!, FontStyle.Regular)
        lblSubTitulo.ForeColor = Color.FromArgb(203, 213, 225)
        lblSubTitulo.Location = New Point(22, 55)
        lblSubTitulo.Size = New Size(520, 20)
        headerPanel.Controls.Add(lblSubTitulo)

        lblBackendBadge = CriarBadge("Backend: parado", Color.Crimson)
        lblBackendBadge.Location = New Point(580, 18)
        headerPanel.Controls.Add(lblBackendBadge)

        lblFrontendBadge = CriarBadge("Frontend: parado", Color.Crimson)
        lblFrontendBadge.Location = New Point(580, 50)
        headerPanel.Controls.Add(lblFrontendBadge)

        lblDbBadge = CriarBadge("Banco: desconhecido", Color.DimGray)
        lblDbBadge.Location = New Point(770, 18)
        headerPanel.Controls.Add(lblDbBadge)

        ' Group: Ações Rápidas
        grpAcoesRapidas = NovoGroupBox("Ações rápidas", New Point(20, 110), New Size(420, 140))
        Me.Controls.Add(grpAcoesRapidas)

        btnIniciarTudo = NovoBotao("🚀 Iniciar tudo", New Point(20, 35), New Size(180, 36), Color.FromArgb(99, 102, 241))
        grpAcoesRapidas.Controls.Add(btnIniciarTudo)

        btnPararTudo = NovoBotao("🛑 Parar tudo", New Point(220, 35), New Size(180, 36), Color.FromArgb(148, 163, 184))
        grpAcoesRapidas.Controls.Add(btnPararTudo)

        btnAbrirFrontend = NovoBotao("🌐 Abrir Frontend", New Point(20, 85), New Size(180, 36), Color.FromArgb(100, 116, 139))
        grpAcoesRapidas.Controls.Add(btnAbrirFrontend)

        btnAbrirSwagger = NovoBotao("📖 Abrir Swagger", New Point(220, 85), New Size(180, 36), Color.FromArgb(100, 116, 139))
        grpAcoesRapidas.Controls.Add(btnAbrirSwagger)

        ' Group: Sincronização
        grpSincronizacao = NovoGroupBox("Sincronização de Dados", New Point(460, 110), New Size(480, 140))
        Me.Controls.Add(grpSincronizacao)

        btnCargaInicial = NovoBotao("Carga Inicial (3 anos)", New Point(20, 35), New Size(200, 36), Color.FromArgb(234, 179, 8))
        grpSincronizacao.Controls.Add(btnCargaInicial)

        btnCargaIncremental = NovoBotao("Carga Incremental", New Point(240, 35), New Size(200, 36), Color.FromArgb(250, 204, 21))
        grpSincronizacao.Controls.Add(btnCargaIncremental)

        btnSyncInteligente = NovoBotao("Sincronização Inteligente", New Point(20, 85), New Size(420, 36), Color.FromArgb(34, 197, 94))
        grpSincronizacao.Controls.Add(btnSyncInteligente)

        ' Group: Atualizações
        grpAtualizacoes = NovoGroupBox("Atualizações", New Point(20, 270), New Size(420, 110))
        Me.Controls.Add(grpAtualizacoes)

        btnVerificarAtualizacoes = NovoBotao("Verificar Atualizações", New Point(20, 35), New Size(180, 36), Color.FromArgb(99, 102, 241))
        grpAtualizacoes.Controls.Add(btnVerificarAtualizacoes)

        btnCriarVersaoTeste = NovoBotao("Criar Versão Teste", New Point(220, 35), New Size(180, 36), Color.FromArgb(100, 116, 139))
        grpAtualizacoes.Controls.Add(btnCriarVersaoTeste)

        ' Group: Portas
        grpPortas = NovoGroupBox("Portas do Sistema", New Point(460, 270), New Size(480, 110))
        Me.Controls.Add(grpPortas)

        btnVerificarPortas = NovoBotao("🔍 Verificar e Liberar Portas", New Point(20, 35), New Size(420, 36), Color.FromArgb(148, 163, 184))
        grpPortas.Controls.Add(btnVerificarPortas)

        ' Group: Banco
        grpBanco = NovoGroupBox("Banco de Dados", New Point(20, 390), New Size(920, 90))
        Me.Controls.Add(grpBanco)

        btnVerificarBanco = NovoBotao("Verificar Estrutura", New Point(20, 30), New Size(160, 36), Color.FromArgb(20, 184, 166))
        grpBanco.Controls.Add(btnVerificarBanco)

        txtBancoInfo = New TextBox()
        txtBancoInfo.Multiline = True
        txtBancoInfo.ReadOnly = True
        txtBancoInfo.ScrollBars = ScrollBars.Vertical
        txtBancoInfo.BackColor = Color.White
        txtBancoInfo.Font = New Font("Consolas", 9.0!)
        txtBancoInfo.Location = New Point(200, 25)
        txtBancoInfo.Size = New Size(700, 50)
        grpBanco.Controls.Add(txtBancoInfo)

        ' Log
        lblStatusOp = New Label()
        lblStatusOp.Text = "Pronto"
        lblStatusOp.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
        lblStatusOp.ForeColor = Color.FromArgb(71, 85, 105)
        lblStatusOp.Location = New Point(20, 490)
        lblStatusOp.Size = New Size(700, 20)
        Me.Controls.Add(lblStatusOp)

        progressBar = New ProgressBar()
        progressBar.Location = New Point(20, 510)
        progressBar.Size = New Size(920, 14)
        progressBar.Style = ProgressBarStyle.Continuous
        progressBar.Value = 0
        Me.Controls.Add(progressBar)

        txtLog = New TextBox()
        txtLog.Multiline = True
        txtLog.ReadOnly = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.BackColor = Color.Black
        txtLog.ForeColor = Color.Lime
        txtLog.Font = New Font("Consolas", 10.0!)
        txtLog.Location = New Point(20, 530)
        txtLog.Size = New Size(920, 170)
        Me.Controls.Add(txtLog)

        ' Eventos de ciclo de vida
        AddHandler Me.Load, AddressOf NovaInterfaceForm_Load
        AddHandler Me.FormClosed, AddressOf NovaInterfaceForm_FormClosed
    End Sub

    Private Function CriarBadge(texto As String, cor As Color) As Label
        Dim lbl As New Label()
        lbl.Text = texto
        lbl.AutoSize = True
        lbl.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        lbl.ForeColor = Color.White
        lbl.BackColor = cor
        lbl.Padding = New Padding(10, 4, 10, 4)
        lbl.BorderStyle = BorderStyle.FixedSingle
        Return lbl
    End Function

    Private Function NovoGroupBox(titulo As String, location As Point, size As Size) As GroupBox
        Dim g As New GroupBox()
        g.Text = titulo
        g.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        g.BackColor = Color.White
        g.Location = location
        g.Size = size
        Return g
    End Function

    Private Function NovoBotao(texto As String, location As Point, size As Size, cor As Color) As Button
        Dim b As New Button()
        b.Text = texto
        b.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        b.ForeColor = Color.White
        b.BackColor = cor
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderSize = 0
        b.Location = location
        b.Size = size
        Return b
    End Function

    ' --- Lifecycle ---
    Private Sub NovaInterfaceForm_Load(sender As Object, e As EventArgs)
        Try
            previousConsoleOut = Console.Out
            consoleWriter = New ConsoleWriter(txtLog)
            Console.SetOut(consoleWriter)
            SetStatus("Pronto")
        Catch
        End Try
    End Sub

    Private Sub NovaInterfaceForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        Try
            If previousConsoleOut IsNot Nothing Then
                Console.SetOut(previousConsoleOut)
            End If
        Catch
        End Try
    End Sub

    ' --- Atualização de Badges ---
    Private Sub AtualizarBadges()
        lblBackendBadge.Text = If(backendApiManager.IsRunning, "Backend: rodando", "Backend: parado")
        lblBackendBadge.BackColor = If(backendApiManager.IsRunning, Color.FromArgb(22, 163, 74), Color.Crimson)

        lblFrontendBadge.Text = If(frontendServer.IsServerRunning, "Frontend: rodando", "Frontend: parado")
        lblFrontendBadge.BackColor = If(frontendServer.IsServerRunning, Color.FromArgb(22, 163, 74), Color.Crimson)

        Try
            Dim dbOk = databaseManager.VerificarExistenciaBanco()
            lblDbBadge.Text = If(dbOk, "Banco: ok", "Banco: ausente")
            lblDbBadge.BackColor = If(dbOk, Color.FromArgb(22, 163, 74), Color.DimGray)
        Catch
            lblDbBadge.Text = "Banco: erro"
            lblDbBadge.BackColor = Color.DimGray
        End Try
    End Sub

    ' --- Eventos de Clique ---
    Private Async Sub btnIniciarTudo_Click(sender As Object, e As EventArgs) Handles btnIniciarTudo.Click
        Try
            Log("Iniciando sistema completo...")
            StartProgress("Iniciando serviços...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(1000)
            Dim okBackend = Await backendApiManager.StartAsync()
            Dim okFrontend = frontendServer.StartAsync()
            If okBackend AndAlso okFrontend Then
                Log("Sistema iniciado com sucesso.")
            Else
                Log("Falha ao iniciar algum componente.")
            End If
        Catch ex As Exception
            Log("Erro: " & ex.Message)
        Finally
            CompleteProgress()
            AtualizarBadges()
        End Try
    End Sub

    Private Sub btnPararTudo_Click(sender As Object, e As EventArgs) Handles btnPararTudo.Click
        Try
            Log("Parando sistema...")
            frontendServer.[Stop]()
            backendApiManager.[Stop]()
            Log("Sistema parado.")
        Catch ex As Exception
            Log("Erro ao parar: " & ex.Message)
        Finally
            AtualizarBadges()
        End Try
    End Sub

    Private Sub btnAbrirFrontend_Click(sender As Object, e As EventArgs) Handles btnAbrirFrontend.Click
        Try
            If frontendServer.IsServerRunning Then
                Process.Start(frontendServer.ServerUrl)
            Else
                MessageBox.Show("Frontend não está rodando", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show("Erro ao abrir frontend: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnAbrirSwagger_Click(sender As Object, e As EventArgs) Handles btnAbrirSwagger.Click
        Try
            If backendApiManager.IsRunning Then
                Process.Start(backendApiManager.SwaggerUrl)
            Else
                MessageBox.Show("Backend não está rodando", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show("Erro ao abrir Swagger: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnVerificarPortas_Click(sender As Object, e As EventArgs) Handles btnVerificarPortas.Click
        Try
            Log("Verificando portas...")
            StartProgress("Verificando portas...")
            portManager.FreeAllSystemPorts()
            Log("Verificação concluída.")
        Catch ex As Exception
            Log("Erro verificação de portas: " & ex.Message)
        Finally
            CompleteProgress()
        End Try
    End Sub

    Private Async Sub btnVerificarAtualizacoes_Click(sender As Object, e As EventArgs) Handles btnVerificarAtualizacoes.Click
        Try
            Log("Verificando atualizações...")
            StartProgress("Verificando atualizações...")
            Dim r = Await updateManager.VerificarAtualizacoes()
            If r.Success Then
                If r.HasUpdate Then
                    Log("Nova versão: " & r.VersionInfo.Version)
                    If MessageBox.Show("Atualizar agora?", "Atualização", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                        Dim ap = Await updateManager.AplicarAtualizacao(r.VersionInfo)
                        MessageBox.Show(ap.Message, If(ap.Success, "Sucesso", "Erro"), MessageBoxButtons.OK, If(ap.Success, MessageBoxIcon.Information, MessageBoxIcon.Error))
                    End If
                Else
                    MessageBox.Show("Sistema está atualizado", "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Else
                MessageBox.Show(r.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            CompleteProgress()
        End Try
    End Sub

    Private Sub btnCriarVersaoTeste_Click(sender As Object, e As EventArgs) Handles btnCriarVersaoTeste.Click
        Try
            updateManager.CriarArquivoVersaoTeste()
            MessageBox.Show("Arquivo de versão de teste criado.", "Ok", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnVerificarBanco_Click(sender As Object, e As EventArgs) Handles btnVerificarBanco.Click
        Try
            Dim r = databaseManager.VerificarEstrutura()
            If r.Success Then
                Dim resumo = $"Tabelas: {r.Tabelas.Count} | " & String.Join("; ", r.Tabelas.Take(6).Select(Function(t) $"{t.Nome}:{t.Registros}"))
                txtBancoInfo.Text = resumo
                MessageBox.Show("Verificação concluída", "Banco", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                txtBancoInfo.Text = r.Mensagem
                MessageBox.Show(r.Mensagem, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            txtBancoInfo.Text = ex.Message
        End Try
        AtualizarBadges()
    End Sub

    Private Sub btnCargaInicial_Click(sender As Object, e As EventArgs) Handles btnCargaInicial.Click
        btnCargaInicial.Enabled = False
        StartProgress("Executando carga inicial...")
        Task.Run(Sub()
                     Try
                         Log("[Sync] Carga inicial iniciada")
                         sincronizador.RealizarCargaInicial()
                         Me.Invoke(Sub()
                                       Log("[Sync] Carga inicial concluída")
                                       MessageBox.Show("Carga inicial concluída", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                   End Sub)
                     Catch ex As Exception
                         Me.Invoke(Sub()
                                       Log("[Sync] Erro: " & ex.Message)
                                       MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                   End Sub)
                     Finally
                         Me.Invoke(Sub()
                                       btnCargaInicial.Enabled = True
                                       CompleteProgress()
                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    Private Sub btnCargaIncremental_Click(sender As Object, e As EventArgs) Handles btnCargaIncremental.Click
        btnCargaIncremental.Enabled = False
        StartProgress("Executando carga incremental...")
        Task.Run(Sub()
                     Try
                         Log("[Sync] Carga incremental iniciada")
                         sincronizador.RealizarCargaIncremental()
                         Me.Invoke(Sub()
                                       Log("[Sync] Carga incremental concluída")
                                       MessageBox.Show("Carga incremental concluída", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                   End Sub)
                     Catch ex As Exception
                         Me.Invoke(Sub()
                                       Log("[Sync] Erro: " & ex.Message)
                                       MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                   End Sub)
                     Finally
                         Me.Invoke(Sub()
                                       btnCargaIncremental.Enabled = True
                                       CompleteProgress()
                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    Private Sub btnSyncInteligente_Click(sender As Object, e As EventArgs) Handles btnSyncInteligente.Click
        btnSyncInteligente.Enabled = False
        StartProgress("Executando sincronização inteligente...")
        Task.Run(Sub()
                     Try
                         Log("[Sync] Sincronização inteligente iniciada")
                         sincronizador.ExecutarSincronizacaoInteligente()
                         Me.Invoke(Sub()
                                       Log("[Sync] Sincronização concluída")
                                       MessageBox.Show("Sincronização concluída", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                   End Sub)
                     Catch ex As Exception
                         Me.Invoke(Sub()
                                       Log("[Sync] Erro: " & ex.Message)
                                       MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                   End Sub)
                     Finally
                         Me.Invoke(Sub()
                                       btnSyncInteligente.Enabled = True
                                       CompleteProgress()
                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    ' --- Eventos dos Managers ---
    Private Sub OnUpdateProgress(p As Integer, s As String)
        Log(s)
        Try
            If progressBar Is Nothing OrElse progressBar.IsDisposed Then Return
            If progressBar.InvokeRequired Then
                progressBar.Invoke(Sub() UpdateProgressValue(p, s))
            Else
                UpdateProgressValue(p, s)
            End If
        Catch
        End Try
    End Sub

    Private Sub OnPortStatusChanged(port As Integer, status As String)
        Log(status)
    End Sub

    Private Sub OnBackendStatusChanged(message As String)
        Log("[Backend] " & message)
    End Sub

    Private Sub OnBackendStarted(url As String)
        Log("Backend iniciado em " & url)
        Me.Invoke(Sub() AtualizarBadges())
    End Sub

    Private Sub OnBackendStopped()
        Log("Backend parado")
        Me.Invoke(Sub() AtualizarBadges())
    End Sub

    Private Sub OnHealthCheck(ok As Boolean, message As String)
        Log("Health: " & message)
    End Sub

    Private Sub OnFrontendStatusChanged(message As String)
        Log("[Frontend] " & message)
    End Sub

    Private Sub OnFrontendStarted(url As String)
        Log("Frontend iniciado em " & url)
        Me.Invoke(Sub() AtualizarBadges())
    End Sub

    Private Sub OnFrontendStopped()
        Log("Frontend parado")
        Me.Invoke(Sub() AtualizarBadges())
    End Sub

    ' --- Util ---
    Private Sub Log(msg As String)
        If txtLog Is Nothing OrElse txtLog.IsDisposed Then Return
        If txtLog.InvokeRequired Then
            txtLog.Invoke(Sub() Log(msg))
        Else
            txtLog.AppendText("[" & DateTime.Now.ToString("HH:mm:ss") & "] " & msg & Environment.NewLine)
            txtLog.SelectionStart = txtLog.TextLength
            txtLog.ScrollToCaret()
        End If
    End Sub

    Private Sub SetStatus(status As String)
        Try
            If lblStatusOp Is Nothing OrElse lblStatusOp.IsDisposed Then Return
            If lblStatusOp.InvokeRequired Then
                lblStatusOp.Invoke(Sub() SetStatus(status))
            Else
                lblStatusOp.Text = status
            End If
        Catch
        End Try
    End Sub

    Private Sub StartProgress(status As String)
        SetStatus(status)
        If progressBar Is Nothing OrElse progressBar.IsDisposed Then Return
        If progressTimer Is Nothing Then
            progressTimer = New Timer()
            progressTimer.Interval = 200
            AddHandler progressTimer.Tick, AddressOf OnProgressTick
        End If
        progressBar.Style = ProgressBarStyle.Continuous
        progressBar.Value = 0
        progressTimer.Start()
        progressActive = True
    End Sub

    Private Sub OnProgressTick(sender As Object, e As EventArgs)
        Try
            If progressBar.Value < 90 Then
                progressBar.Value = progressBar.Value + 2
            Else
                progressBar.Value = 60 ' loop suave para indicar atividade
            End If
        Catch
            ' Ignorar se barra não estiver pronta
        End Try
    End Sub

    Private Sub CompleteProgress()
        Try
            If progressTimer IsNot Nothing Then
                progressTimer.Stop()
            End If
            If progressBar IsNot Nothing AndAlso Not progressBar.IsDisposed Then
                progressBar.Value = 100
            End If
            SetStatus("Pronto")
        Catch
        End Try
        progressActive = False
    End Sub

    Private Sub UpdateProgressValue(p As Integer, s As String)
        Try
            SetStatus(s)
            If p < 0 Then p = 0
            If p > 100 Then p = 100
            If progressTimer IsNot Nothing Then progressTimer.Stop()
            progressBar.Style = ProgressBarStyle.Continuous
            progressBar.Value = p
            If p >= 100 Then
                SetStatus("Pronto")
                progressActive = False
            End If
        Catch
        End Try
    End Sub
End Class


