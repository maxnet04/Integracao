Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading.Tasks

Public Class NovaInterfaceAvancadaForm
    Inherits Form

    ' Managers
    Private ReadOnly updateManager As UpdateManager
    Private ReadOnly portManager As PortManager
    Private ReadOnly backendApiManager As BackendApiManager
    Private ReadOnly frontendServer As FrontendHttpServer
    Private ReadOnly sincronizador As SincronizadorDados
    Private ReadOnly databaseManager As DatabaseManager

    ' Layout containers
    Private headerPanel As Panel
    Private leftPanel As Panel
    Private rightPanel As Panel
    Private footerPanel As Panel

    ' Header badges
    Private lblTitulo As Label
    Private lblBackendBadge As Label
    Private lblFrontendBadge As Label
    Private lblDbBadge As Label

    ' Left groups
    Private grpSistema As GroupBox
    Private WithEvents btnIniciarTudo As Button
    Private WithEvents btnPararTudo As Button
    Private WithEvents btnAbrirFrontend As Button
    Private WithEvents btnAbrirSwagger As Button
    Private WithEvents btnIniciarAPI As Button
    Private WithEvents btnHealthCheck As Button

    Private grpSincronizacao As GroupBox
    Private WithEvents btnCargaInicial As Button
    Private WithEvents btnCargaIncremental As Button
    Private WithEvents btnSyncInteligente As Button

    Private grpUtilitarios As GroupBox
    Private WithEvents btnVerificarPortas As Button
    Private WithEvents btnVerificarAtualizacoes As Button
    Private WithEvents btnCriarVersaoTeste As Button

    ' Right: log toolbar + log view
    Private toolbarPanel As Panel
    Private txtSearch As TextBox
    Private WithEvents btnFindNext As Button
    Private WithEvents btnCopy As Button
    Private WithEvents btnClear As Button
    Private WithEvents btnExport As Button
    Private chkAutoScroll As CheckBox
    Private logs As RichTextBox

    ' Footer
    Private lblStatus As Label
    Private progressBar As ProgressBar
    Private progressTimer As Timer

    ' Console redirection
    Private previousConsoleOut As TextWriter
    Private consoleWriter As RichTextConsoleWriter

    Public Sub New()
        updateManager = New UpdateManager()
        portManager = New PortManager()
        backendApiManager = New BackendApiManager()
        Dim frontendBuildPath = Path.Combine(Application.StartupPath, "frontend", "build")
        frontendServer = New FrontendHttpServer(frontendBuildPath, 8080)
        sincronizador = New SincronizadorDados()
        databaseManager = New DatabaseManager()

        AddHandler updateManager.ProgressChanged, AddressOf OnUpdateProgress
        AddHandler portManager.PortStatusChanged, AddressOf OnPortStatus
        AddHandler backendApiManager.StatusChanged, AddressOf OnBackendStatus
        AddHandler backendApiManager.ServerStarted, AddressOf OnBackendStarted
        AddHandler backendApiManager.ServerStopped, AddressOf OnBackendStopped
        AddHandler frontendServer.StatusChanged, AddressOf OnFrontendStatus
        AddHandler frontendServer.ServerStarted, AddressOf OnFrontendStarted
        AddHandler frontendServer.ServerStopped, AddressOf OnFrontendStopped

        InitializeComponent()
        AtualizarBadges()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "SUAT-IA - Interface Avançada"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.ClientSize = New Size(1100, 720)
        Me.BackColor = Color.FromArgb(248, 250, 252)

        ' Header
        headerPanel = New Panel()
        headerPanel.BackColor = Color.FromArgb(51, 65, 85)
        headerPanel.Size = New Size(Me.ClientSize.Width, 70)
        headerPanel.Location = New Point(0, 0)
        headerPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(headerPanel)

        lblTitulo = New Label()
        lblTitulo.Text = "SUAT-IA | Monitor com Logs"
        lblTitulo.Font = New Font("Segoe UI", 14.0!, FontStyle.Bold)
        lblTitulo.ForeColor = Color.White
        lblTitulo.Location = New Point(20, 20)
        lblTitulo.AutoSize = True
        headerPanel.Controls.Add(lblTitulo)

        lblBackendBadge = CriarBadge("Backend: parado", Color.Crimson)
        lblBackendBadge.Location = New Point(650, 22)
        headerPanel.Controls.Add(lblBackendBadge)

        lblFrontendBadge = CriarBadge("Frontend: parado", Color.Crimson)
        lblFrontendBadge.Location = New Point(800, 22)
        headerPanel.Controls.Add(lblFrontendBadge)

        lblDbBadge = CriarBadge("Banco: desconhecido", Color.DimGray)
        lblDbBadge.Location = New Point(950, 22)
        headerPanel.Controls.Add(lblDbBadge)

        ' Left panel (30-35%)
        leftPanel = New Panel()
        leftPanel.BackColor = Color.White
        leftPanel.Location = New Point(0, 70)
        leftPanel.Size = New Size(360, 600)
        leftPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        Me.Controls.Add(leftPanel)

        ' Right panel (65-70%)
        rightPanel = New Panel()
        rightPanel.BackColor = Color.White
        rightPanel.Location = New Point(360, 70)
        rightPanel.Size = New Size(740, 600)
        rightPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(rightPanel)

        ' Footer
        footerPanel = New Panel()
        footerPanel.BackColor = Color.FromArgb(241, 245, 249)
        footerPanel.Location = New Point(0, 670)
        footerPanel.Size = New Size(Me.ClientSize.Width, 50)
        footerPanel.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        Me.Controls.Add(footerPanel)

        lblStatus = New Label()
        lblStatus.Text = "Pronto"
        lblStatus.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
        lblStatus.ForeColor = Color.FromArgb(71, 85, 105)
        lblStatus.Location = New Point(20, 8)
        lblStatus.Size = New Size(700, 16)
        footerPanel.Controls.Add(lblStatus)

        progressBar = New ProgressBar()
        progressBar.Location = New Point(20, 26)
        progressBar.Size = New Size(1060, 12)
        progressBar.Style = ProgressBarStyle.Continuous
        footerPanel.Controls.Add(progressBar)

        ' Groups - Sistema
        grpSistema = NovoGroupBox("Sistema", New Point(20, 10), New Size(320, 190))
        leftPanel.Controls.Add(grpSistema)

        btnIniciarTudo = NovoBotao("🚀 Iniciar tudo", New Point(15, 30), New Size(135, 32), Color.FromArgb(99, 102, 241))
        grpSistema.Controls.Add(btnIniciarTudo)

        btnPararTudo = NovoBotao("🛑 Parar tudo", New Point(170, 30), New Size(135, 32), Color.FromArgb(148, 163, 184))
        grpSistema.Controls.Add(btnPararTudo)

        btnAbrirFrontend = NovoBotao("🌐 Frontend", New Point(15, 80), New Size(135, 32), Color.FromArgb(100, 116, 139))
        grpSistema.Controls.Add(btnAbrirFrontend)

        btnAbrirSwagger = NovoBotao("📖 Swagger", New Point(170, 80), New Size(135, 32), Color.FromArgb(100, 116, 139))
        grpSistema.Controls.Add(btnAbrirSwagger)

        btnIniciarAPI = NovoBotao("API: Iniciar", New Point(15, 120), New Size(135, 32), Color.FromArgb(59, 130, 246))
        grpSistema.Controls.Add(btnIniciarAPI)

        btnHealthCheck = NovoBotao("API: Health", New Point(170, 120), New Size(135, 32), Color.FromArgb(250, 204, 21))
        grpSistema.Controls.Add(btnHealthCheck)

        ' Groups - Sincronização
        grpSincronizacao = NovoGroupBox("Sincronização", New Point(20, 210), New Size(320, 150))
        leftPanel.Controls.Add(grpSincronizacao)

        btnCargaInicial = NovoBotao("Carga Inicial", New Point(15, 30), New Size(135, 32), Color.FromArgb(234, 179, 8))
        grpSincronizacao.Controls.Add(btnCargaInicial)

        btnCargaIncremental = NovoBotao("Incremental", New Point(170, 30), New Size(135, 32), Color.FromArgb(250, 204, 21))
        grpSincronizacao.Controls.Add(btnCargaIncremental)

        btnSyncInteligente = NovoBotao("Sincronização Inteligente", New Point(15, 80), New Size(290, 32), Color.FromArgb(34, 197, 94))
        grpSincronizacao.Controls.Add(btnSyncInteligente)

        ' Groups - Utilitários
        grpUtilitarios = NovoGroupBox("Utilitários", New Point(20, 370), New Size(320, 150))
        leftPanel.Controls.Add(grpUtilitarios)

        btnVerificarPortas = NovoBotao("🔍 Verificar Portas", New Point(15, 30), New Size(290, 32), Color.FromArgb(148, 163, 184))
        grpUtilitarios.Controls.Add(btnVerificarPortas)

        btnVerificarAtualizacoes = NovoBotao("Atualizações", New Point(15, 80), New Size(180, 32), Color.FromArgb(99, 102, 241))
        grpUtilitarios.Controls.Add(btnVerificarAtualizacoes)

        btnCriarVersaoTeste = NovoBotao("Criar Versão Teste", New Point(205, 80), New Size(100, 32), Color.FromArgb(100, 116, 139))
        grpUtilitarios.Controls.Add(btnCriarVersaoTeste)

        ' Toolbar de logs
        toolbarPanel = New Panel()
        toolbarPanel.BackColor = Color.FromArgb(248, 250, 252)
        toolbarPanel.Location = New Point(0, 0)
        toolbarPanel.Size = New Size(740, 44)
        toolbarPanel.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        rightPanel.Controls.Add(toolbarPanel)

        txtSearch = New TextBox()
        txtSearch.Font = New Font("Segoe UI", 9.0!)
        txtSearch.Location = New Point(16, 10)
        txtSearch.Size = New Size(220, 23)
        toolbarPanel.Controls.Add(txtSearch)

        Dim tt As New ToolTip()
        tt.SetToolTip(txtSearch, "Buscar nos logs (Enter/Encontrar)")

        btnFindNext = New Button()
        btnFindNext.Text = "Encontrar"
        btnFindNext.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnFindNext.BackColor = Color.FromArgb(148, 163, 184)
        btnFindNext.ForeColor = Color.White
        btnFindNext.FlatStyle = FlatStyle.Flat
        btnFindNext.FlatAppearance.BorderSize = 0
        btnFindNext.Location = New Point(242, 9)
        btnFindNext.Size = New Size(90, 25)
        toolbarPanel.Controls.Add(btnFindNext)

        btnCopy = New Button()
        btnCopy.Text = "Copiar"
        btnCopy.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnCopy.BackColor = Color.FromArgb(100, 116, 139)
        btnCopy.ForeColor = Color.White
        btnCopy.FlatStyle = FlatStyle.Flat
        btnCopy.FlatAppearance.BorderSize = 0
        btnCopy.Location = New Point(338, 9)
        btnCopy.Size = New Size(80, 25)
        toolbarPanel.Controls.Add(btnCopy)

        btnClear = New Button()
        btnClear.Text = "Limpar"
        btnClear.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnClear.BackColor = Color.FromArgb(239, 68, 68)
        btnClear.ForeColor = Color.White
        btnClear.FlatStyle = FlatStyle.Flat
        btnClear.FlatAppearance.BorderSize = 0
        btnClear.Location = New Point(424, 9)
        btnClear.Size = New Size(80, 25)
        toolbarPanel.Controls.Add(btnClear)

        btnExport = New Button()
        btnExport.Text = "Exportar"
        btnExport.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnExport.BackColor = Color.FromArgb(20, 184, 166)
        btnExport.ForeColor = Color.White
        btnExport.FlatStyle = FlatStyle.Flat
        btnExport.FlatAppearance.BorderSize = 0
        btnExport.Location = New Point(510, 9)
        btnExport.Size = New Size(90, 25)
        toolbarPanel.Controls.Add(btnExport)

        chkAutoScroll = New CheckBox()
        chkAutoScroll.Text = "Autoscroll"
        chkAutoScroll.Checked = True
        chkAutoScroll.Location = New Point(610, 12)
        toolbarPanel.Controls.Add(chkAutoScroll)

        ' Logs viewer
        logs = New RichTextBox()
        logs.BackColor = Color.Black
        logs.ForeColor = Color.Lime
        logs.Font = New Font("Consolas", 10.0!)
        logs.ReadOnly = True
        logs.Location = New Point(0, 44)
        logs.Size = New Size(740, 556)
        logs.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
        logs.BorderStyle = BorderStyle.None
        rightPanel.Controls.Add(logs)

        AddHandler Me.Load, AddressOf OnLoadForm
        AddHandler Me.FormClosed, AddressOf NovaInterfaceAvancadaForm_FormClosed
    End Sub

    Private Function CriarBadge(texto As String, cor As Color) As Label
        Dim l As New Label()
        l.Text = texto
        l.AutoSize = True
        l.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        l.ForeColor = Color.White
        l.BackColor = cor
        l.Padding = New Padding(8, 3, 8, 3)
        l.BorderStyle = BorderStyle.FixedSingle
        Return l
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

    ' Lifecycle
    Private Sub OnLoadForm(sender As Object, e As EventArgs)
        previousConsoleOut = Console.Out
        consoleWriter = New RichTextConsoleWriter(logs, chkAutoScroll)
        Console.SetOut(consoleWriter)
        SetStatus("Pronto")
    End Sub

    Private Sub NovaInterfaceAvancadaForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        Try
            If previousConsoleOut IsNot Nothing Then
                Console.SetOut(previousConsoleOut)
            End If
        Catch
        End Try
    End Sub

    ' Status helpers
    Private Sub SetStatus(texto As String)
        lblStatus.Text = texto
    End Sub

    Private Sub StartProgress(texto As String)
        SetStatus(texto)
        If progressTimer Is Nothing Then
            progressTimer = New Timer()
            progressTimer.Interval = 200
            AddHandler progressTimer.Tick, Sub()
                                               Try
                                                   If progressBar.Value < 90 Then
                                                       progressBar.Value += 2
                                                   Else
                                                       progressBar.Value = 60
                                                   End If
                                               Catch
                                               End Try
                                           End Sub
        End If
        progressBar.Value = 0
        progressTimer.Start()
    End Sub

    Private Sub CompleteProgress()
        Try
            If progressTimer IsNot Nothing Then
                progressTimer.Stop()
            End If
            progressBar.Value = 100
            SetStatus("Pronto")
        Catch
        End Try
    End Sub

    ' Event wiring from managers
    Private Sub OnUpdateProgress(p As Integer, s As String)
        Log(s)
        Try
            If p < 0 Then p = 0
            If p > 100 Then p = 100
            progressBar.Value = p
            If p >= 100 Then
                SetStatus("Pronto")
            Else
                SetStatus(s)
            End If
        Catch
        End Try
    End Sub

    Private Sub OnPortStatus(port As Integer, status As String)
        Log(status)
    End Sub

    Private Sub OnBackendStatus(message As String)
        Log("[Backend] " & message)
    End Sub

    Private Sub OnBackendStarted(url As String)
        Log("Backend iniciado em " & url)
        AtualizarBadges()
    End Sub

    Private Sub OnBackendStopped()
        Log("Backend parado")
        AtualizarBadges()
    End Sub

    Private Sub OnFrontendStatus(message As String)
        Log("[Frontend] " & message)
    End Sub

    Private Sub OnFrontendStarted(url As String)
        Log("Frontend iniciado em " & url)
        AtualizarBadges()
    End Sub

    Private Sub OnFrontendStopped()
        Log("Frontend parado")
        AtualizarBadges()
    End Sub

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

    ' Toolbar actions
    Private Sub btnFindNext_Click(sender As Object, e As EventArgs) Handles btnFindNext.Click
        Try
            Dim query = txtSearch.Text
            If String.IsNullOrWhiteSpace(query) Then Return
            Dim startIndex = logs.SelectionStart + logs.SelectionLength
            Dim idx = logs.Text.IndexOf(query, startIndex, StringComparison.OrdinalIgnoreCase)
            If idx = -1 AndAlso startIndex > 0 Then
                idx = logs.Text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase)
            End If
            If idx >= 0 Then
                logs.Select(idx, query.Length)
                logs.ScrollToCaret()
                logs.Focus()
            End If
        Catch
        End Try
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As EventArgs) Handles btnCopy.Click
        Try
            If logs.SelectionLength > 0 Then
                Clipboard.SetText(logs.SelectedText)
            Else
                Clipboard.SetText(logs.Text)
            End If
        Catch
        End Try
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        logs.Clear()
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Try
            Dim sfd As New SaveFileDialog()
            sfd.Filter = "Arquivo de texto (*.txt)|*.txt"
            sfd.FileName = "suat-logs-" & DateTime.Now.ToString("yyyyMMdd-HHmmss") & ".txt"
            If sfd.ShowDialog() = DialogResult.OK Then
                File.WriteAllText(sfd.FileName, logs.Text)
                MessageBox.Show("Exportado: " & sfd.FileName, "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show("Erro ao exportar: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Left actions
    Private Async Sub btnIniciarTudo_Click(sender As Object, e As EventArgs) Handles btnIniciarTudo.Click
        Try
            StartProgress("Iniciando serviços...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(1000)
            Dim okBackend = Await backendApiManager.StartAsync()
            Dim okFrontend = frontendServer.StartAsync()
            If okBackend AndAlso okFrontend Then
                Log("Sistema iniciado")
            Else
                Log("Falha ao iniciar algum componente")
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
            frontendServer.[Stop]()
            backendApiManager.[Stop]()
            Log("Sistema parado")
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

    Private Async Sub btnIniciarAPI_Click(sender As Object, e As EventArgs) Handles btnIniciarAPI.Click
        Try
            StartProgress("Iniciando API...")
            Dim ok = Await backendApiManager.StartAsync()
            If ok Then
                Log("API iniciada")
            Else
                Log("Falha ao iniciar API")
            End If
        Catch ex As Exception
            Log("Erro ao iniciar API: " & ex.Message)
        Finally
            CompleteProgress()
            AtualizarBadges()
        End Try
    End Sub

    Private Sub btnHealthCheck_Click(sender As Object, e As EventArgs) Handles btnHealthCheck.Click
        Try
            backendApiManager.CheckHealthAsync()
        Catch ex As Exception
            Log("Erro no health check: " & ex.Message)
        End Try
    End Sub

    Private Sub btnVerificarPortas_Click(sender As Object, e As EventArgs) Handles btnVerificarPortas.Click
        Try
            StartProgress("Verificando portas...")
            portManager.FreeAllSystemPorts()
            Log("Portas verificadas")
        Catch ex As Exception
            Log("Erro ao verificar portas: " & ex.Message)
        Finally
            CompleteProgress()
        End Try
    End Sub

    Private Async Sub btnVerificarAtualizacoes_Click(sender As Object, e As EventArgs) Handles btnVerificarAtualizacoes.Click
        Try
            StartProgress("Verificando atualizações...")
            Dim r = Await updateManager.VerificarAtualizacoes()
            If r.Success Then
                If r.HasUpdate Then
                    Log("Nova versão: " & r.VersionInfo.Version)
                Else
                    Log("Sistema está atualizado")
                End If
            Else
                Log("Erro: " & r.Message)
            End If
        Catch ex As Exception
            Log("Erro: " & ex.Message)
        Finally
            CompleteProgress()
        End Try
    End Sub

    Private Sub btnCriarVersaoTeste_Click(sender As Object, e As EventArgs) Handles btnCriarVersaoTeste.Click
        Try
            updateManager.CriarArquivoVersaoTeste()
            MessageBox.Show("Arquivo de versão criado", "Atualizações", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnCargaInicial_Click(sender As Object, e As EventArgs) Handles btnCargaInicial.Click
        btnCargaInicial.Enabled = False
        StartProgress("Carga inicial...")
        Task.Run(Sub()
                     Try
                         sincronizador.RealizarCargaInicial()
                         Me.Invoke(Sub() MessageBox.Show("Carga inicial concluída", "Sincronização", MessageBoxButtons.OK, MessageBoxIcon.Information))
                     Catch ex As Exception
                         Me.Invoke(Sub() MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error))
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
        StartProgress("Carga incremental...")
        Task.Run(Sub()
                     Try
                         sincronizador.RealizarCargaIncremental()
                         Me.Invoke(Sub() MessageBox.Show("Carga incremental concluída", "Sincronização", MessageBoxButtons.OK, MessageBoxIcon.Information))
                     Catch ex As Exception
                         Me.Invoke(Sub() MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error))
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
        StartProgress("Sincronização inteligente...")
        Task.Run(Sub()
                     Try
                         sincronizador.ExecutarSincronizacaoInteligente()
                         Me.Invoke(Sub() MessageBox.Show("Sincronização concluída", "Sincronização", MessageBoxButtons.OK, MessageBoxIcon.Information))
                     Catch ex As Exception
                         Me.Invoke(Sub() MessageBox.Show("Erro: " & ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error))
                     Finally
                         Me.Invoke(Sub()
                                       btnSyncInteligente.Enabled = True
                                       CompleteProgress()
                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    ' Logging
    Private Sub Log(message As String)
        Try
            logs.AppendText("[" & DateTime.Now.ToString("HH:mm:ss") & "] " & message & Environment.NewLine)
            If chkAutoScroll.Checked Then
                logs.SelectionStart = logs.TextLength
                logs.ScrollToCaret()
            End If
        Catch
        End Try
    End Sub

End Class

' RichTextBox writer for Console redirection
Public Class RichTextConsoleWriter
    Inherits TextWriter

    Private ReadOnly _box As RichTextBox
    Private ReadOnly _auto As CheckBox

    Public Sub New(box As RichTextBox, auto As CheckBox)
        _box = box
        _auto = auto
    End Sub

    Public Overrides ReadOnly Property Encoding As System.Text.Encoding
        Get
            Return System.Text.Encoding.UTF8
        End Get
    End Property

    Public Overrides Sub Write(value As String)
        Try
            If _box IsNot Nothing AndAlso Not _box.IsDisposed Then
                If _box.InvokeRequired Then
                    _box.Invoke(Sub() WriteToBox(value))
                Else
                    WriteToBox(value)
                End If
            End If
        Catch
        End Try
    End Sub

    Public Overrides Sub WriteLine(value As String)
        Write(value & Environment.NewLine)
    End Sub

    Private Sub WriteToBox(value As String)
        Try
            _box.AppendText(value)
            If _auto IsNot Nothing AndAlso _auto.Checked Then
                _box.SelectionStart = _box.TextLength
                _box.ScrollToCaret()
            End If
            Application.DoEvents()
        Catch
        End Try
    End Sub
End Class


