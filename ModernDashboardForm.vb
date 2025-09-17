Imports System
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Threading.Tasks

''' <summary>
''' Dashboard moderno com Material Design e animações fluidas
''' Layout responsivo e experiência visual impressionante
''' </summary>
Public Class ModernDashboardForm
    Inherits Form

    ' Managers
    Private ReadOnly updateManager As UpdateManager
    Private ReadOnly portManager As PortManager
    Private ReadOnly backendApiManager As BackendApiManager
    Private ReadOnly frontendServer As FrontendHttpServer
    Private ReadOnly sincronizador As SincronizadorDados
    Private ReadOnly databaseManager As DatabaseManager

    ' Layout containers
    Private topBar As Panel
    Private sidebar As Panel
    Private mainContent As Panel
    Private statusFooter As Panel

    ' Top bar elements
    Private lblTitle As Label
    Private btnMinimize As Button
    Private btnClose As Button
    Private systemStatusPanel As Panel
    Private backendStatusCard As Panel
    Private frontendStatusCard As Panel
    Private dbStatusCard As Panel

    ' Sidebar navigation
    Private navDashboard As Panel
    Private navSystem As Panel
    Private navSync As Panel
    Private navLogs As Panel
    Private navSettings As Panel

    ' Main content cards
    Private quickActionsCard As Panel
    Private syncCard As Panel
    Private monitoringCard As Panel
    Private logsCard As Panel

    ' Status and progress
    Private operationStatus As Label
    Private modernProgress As Panel
    Private progressValue As Integer = 0
    Private progressTimer As Timer

    ' Console redirection
    Private previousConsoleOut As TextWriter
    Private consoleWriter As ModernConsoleWriter
    Private logsText As RichTextBox

    ' Animation
    Private animationTimer As Timer
    Private pulseTimer As Timer

    Public Sub New()
        ' Initialize managers
        updateManager = New UpdateManager()
        portManager = New PortManager()
        backendApiManager = New BackendApiManager()
        Dim frontendBuildPath = Path.Combine(Application.StartupPath, "frontend", "build")
        frontendServer = New FrontendHttpServer(frontendBuildPath, 8080)
        sincronizador = New SincronizadorDados()
        databaseManager = New DatabaseManager()

        ' Wire events
        AddHandler updateManager.ProgressChanged, AddressOf OnProgress
        AddHandler portManager.PortStatusChanged, AddressOf OnPortStatus
        AddHandler backendApiManager.StatusChanged, AddressOf OnBackendStatus
        AddHandler backendApiManager.ServerStarted, AddressOf OnBackendStarted
        AddHandler backendApiManager.ServerStopped, AddressOf OnBackendStopped
        AddHandler frontendServer.StatusChanged, AddressOf OnFrontendStatus
        AddHandler frontendServer.ServerStarted, AddressOf OnFrontendStarted
        AddHandler frontendServer.ServerStopped, AddressOf OnFrontendStopped

        InitializeModernUI()
        SetupAnimations()
        UpdateStatusCards()
    End Sub

    Private Sub InitializeModernUI()
        ' Form setup
        Me.Text = ""
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(1200, 800)
        Me.BackColor = Color.FromArgb(15, 23, 42) ' Dark slate
        Me.DoubleBuffered = True

        ' Top bar with gradient
        topBar = New Panel()
        topBar.Size = New Size(Me.Width, 60)
        topBar.Location = New Point(0, 0)
        topBar.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler topBar.Paint, AddressOf PaintTopBarGradient
        Me.Controls.Add(topBar)

        ' Title with modern font
        lblTitle = New Label()
        lblTitle.Text = "SUAT-IA • Modern Dashboard"
        lblTitle.Font = New Font("Segoe UI", 16.0!, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.Location = New Point(20, 15)
        lblTitle.AutoSize = True
        topBar.Controls.Add(lblTitle)

        ' Window controls
        btnClose = CreateWindowButton("✕", Color.FromArgb(239, 68, 68))
        btnClose.Location = New Point(Me.Width - 50, 15)
        AddHandler btnClose.Click, Sub() Me.Close()
        topBar.Controls.Add(btnClose)

        btnMinimize = CreateWindowButton("—", Color.FromArgb(148, 163, 184))
        btnMinimize.Location = New Point(Me.Width - 90, 15)
        AddHandler btnMinimize.Click, Sub() Me.WindowState = FormWindowState.Minimized
        topBar.Controls.Add(btnMinimize)

        ' System status cards in top bar
        systemStatusPanel = New Panel()
        systemStatusPanel.Size = New Size(600, 40)
        systemStatusPanel.Location = New Point(400, 10)
        systemStatusPanel.BackColor = Color.Transparent
        topBar.Controls.Add(systemStatusPanel)

        backendStatusCard = CreateStatusCard("Backend", "Parado", Color.FromArgb(239, 68, 68))
        backendStatusCard.Location = New Point(0, 0)
        systemStatusPanel.Controls.Add(backendStatusCard)

        frontendStatusCard = CreateStatusCard("Frontend", "Parado", Color.FromArgb(239, 68, 68))
        frontendStatusCard.Location = New Point(150, 0)
        systemStatusPanel.Controls.Add(frontendStatusCard)

        dbStatusCard = CreateStatusCard("Database", "Verificando", Color.FromArgb(148, 163, 184))
        dbStatusCard.Location = New Point(300, 0)
        systemStatusPanel.Controls.Add(dbStatusCard)

        ' Sidebar with beautiful navigation
        sidebar = New Panel()
        sidebar.Size = New Size(250, Me.Height - 60)
        sidebar.Location = New Point(0, 60)
        sidebar.BackColor = Color.FromArgb(30, 41, 59) ' Slate 800
        sidebar.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        Me.Controls.Add(sidebar)

        CreateNavigationMenu()

        ' Main content area
        mainContent = New Panel()
        mainContent.Size = New Size(Me.Width - 250, Me.Height - 110)
        mainContent.Location = New Point(250, 60)
        mainContent.BackColor = Color.FromArgb(15, 23, 42)
        mainContent.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(mainContent)

        CreateMainContentCards()

        ' Status footer
        statusFooter = New Panel()
        statusFooter.Size = New Size(Me.Width, 50)
        statusFooter.Location = New Point(0, Me.Height - 50)
        statusFooter.BackColor = Color.FromArgb(30, 41, 59)
        statusFooter.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(statusFooter)

        operationStatus = New Label()
        operationStatus.Text = "Pronto para operação"
        operationStatus.Font = New Font("Segoe UI", 10.0!)
        operationStatus.ForeColor = Color.FromArgb(148, 163, 184)
        operationStatus.Location = New Point(20, 15)
        operationStatus.AutoSize = True
        statusFooter.Controls.Add(operationStatus)

        ' Modern progress bar
        modernProgress = New Panel()
        modernProgress.Size = New Size(Me.Width - 40, 6)
        modernProgress.Location = New Point(20, 35)
        modernProgress.BackColor = Color.FromArgb(51, 65, 85)
        modernProgress.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler modernProgress.Paint, AddressOf PaintModernProgress
        statusFooter.Controls.Add(modernProgress)

        AddHandler Me.Load, AddressOf OnFormLoad
        AddHandler Me.FormClosed, AddressOf OnFormClosed
    End Sub

    Private Function CreateWindowButton(text As String, color As Color) As Button
        Dim btn As New Button()
        btn.Text = text
        btn.Size = New Size(30, 30)
        btn.BackColor = color
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font = New Font("Segoe UI", 12.0!, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        Return btn
    End Function

    Private Function CreateStatusCard(title As String, status As String, color As Color) As Panel
        Dim card As New Panel()
        card.Size = New Size(140, 40)
        card.BackColor = Color.FromArgb(51, 65, 85)
        AddHandler card.Paint, Sub(s, e) PaintStatusCard(e.Graphics, card, title, status, color)
        Return card
    End Function

    Private Sub PaintStatusCard(g As Graphics, card As Panel, title As String, status As String, color As Color)
        g.SmoothingMode = SmoothingMode.AntiAlias
        
        ' Card background with rounded corners
        Dim rect As New Rectangle(0, 0, card.Width, card.Height)
        Using path As GraphicsPath = CreateRoundedPath(rect, 8)
            Using brush As New SolidBrush(Color.FromArgb(51, 65, 85))
                g.FillPath(brush, path)
            End Using
        End Using

        ' Status indicator dot
        Using brush As New SolidBrush(color)
            g.FillEllipse(brush, 10, 15, 8, 8)
        End Using

        ' Title
        Using font As New Font("Segoe UI", 8.0!, FontStyle.Bold)
            Using brush As New SolidBrush(Color.White)
                g.DrawString(title, font, brush, 25, 8)
            End Using
        End Using

        ' Status
        Using font As New Font("Segoe UI", 7.0!)
            Using brush As New SolidBrush(Color.FromArgb(148, 163, 184))
                g.DrawString(status, font, brush, 25, 22)
            End Using
        End Using
    End Sub

    Private Sub CreateNavigationMenu()
        Dim navItems = {
            ("🏠", "Dashboard", True),
            ("⚡", "Sistema", False),
            ("🔄", "Sincronização", False),
            ("📊", "Logs", False),
            ("⚙️", "Configurações", False)
        }

        For i As Integer = 0 To navItems.Length - 1
            Dim item = navItems(i)
            Dim navItem = CreateNavItem(item.Item1, item.Item2, item.Item3)
            navItem.Location = New Point(0, 50 + (i * 60))
            sidebar.Controls.Add(navItem)
        Next
    End Sub

    Private Function CreateNavItem(icon As String, text As String, isActive As Boolean) As Panel
        Dim item As New Panel()
        item.Size = New Size(250, 50)
        item.BackColor = If(isActive, Color.FromArgb(99, 102, 241), Color.Transparent)
        item.Cursor = Cursors.Hand

        Dim iconLabel As New Label()
        iconLabel.Text = icon
        iconLabel.Font = New Font("Segoe UI", 16.0!)
        iconLabel.ForeColor = Color.White
        iconLabel.Location = New Point(20, 12)
        iconLabel.AutoSize = True
        item.Controls.Add(iconLabel)

        Dim textLabel As New Label()
        textLabel.Text = text
        textLabel.Font = New Font("Segoe UI", 11.0!, FontStyle.Bold)
        textLabel.ForeColor = Color.White
        textLabel.Location = New Point(60, 15)
        textLabel.AutoSize = True
        item.Controls.Add(textLabel)

        Return item
    End Function

    Private Sub CreateMainContentCards()
        ' Quick Actions Card
        quickActionsCard = CreateModernCard("Ações Rápidas", New Point(20, 20), New Size(440, 200))
        CreateQuickActionButtons(quickActionsCard)
        mainContent.Controls.Add(quickActionsCard)

        ' Sync Card
        syncCard = CreateModernCard("Sincronização de Dados", New Point(480, 20), New Size(440, 200))
        CreateSyncButtons(syncCard)
        mainContent.Controls.Add(syncCard)

        ' Monitoring Card
        monitoringCard = CreateModernCard("Monitoramento do Sistema", New Point(20, 240), New Size(440, 200))
        CreateMonitoringContent(monitoringCard)
        mainContent.Controls.Add(monitoringCard)

        ' Logs Card
        logsCard = CreateModernCard("Logs em Tempo Real", New Point(480, 240), New Size(440, 200))
        CreateLogsViewer(logsCard)
        mainContent.Controls.Add(logsCard)
    End Sub

    Private Function CreateModernCard(title As String, location As Point, size As Size) As Panel
        Dim card As New Panel()
        card.Location = location
        card.Size = size
        card.BackColor = Color.FromArgb(30, 41, 59)
        AddHandler card.Paint, Sub(s, e) PaintModernCard(e.Graphics, card, title)

        Return card
    End Function

    Private Sub PaintModernCard(g As Graphics, card As Panel, title As String)
        g.SmoothingMode = SmoothingMode.AntiAlias

        ' Card shadow effect
        Dim shadowRect As New Rectangle(2, 2, card.Width, card.Height)
        Using shadowBrush As New SolidBrush(Color.FromArgb(30, 0, 0, 0))
            Using shadowPath As GraphicsPath = CreateRoundedPath(shadowRect, 12)
                g.FillPath(shadowBrush, shadowPath)
            End Using
        End Using

        ' Card background
        Dim cardRect As New Rectangle(0, 0, card.Width, card.Height)
        Using cardBrush As New LinearGradientBrush(cardRect, Color.FromArgb(30, 41, 59), Color.FromArgb(51, 65, 85), LinearGradientMode.Vertical)
            Using cardPath As GraphicsPath = CreateRoundedPath(cardRect, 12)
                g.FillPath(cardBrush, cardPath)
            End Using
        End Using

        ' Title
        Using font As New Font("Segoe UI", 14.0!, FontStyle.Bold)
            Using brush As New SolidBrush(Color.White)
                g.DrawString(title, font, brush, 20, 15)
            End Using
        End Using

        ' Accent line
        Using pen As New Pen(Color.FromArgb(99, 102, 241), 3)
            g.DrawLine(pen, 20, 45, card.Width - 20, 45)
        End Using
    End Sub

    Private Sub CreateQuickActionButtons(parent As Panel)
        Dim startBtn = CreateModernButton("🚀 Iniciar Sistema", Color.FromArgb(34, 197, 94))
        startBtn.Location = New Point(20, 60)
        AddHandler startBtn.Click, AddressOf OnStartSystem
        parent.Controls.Add(startBtn)

        Dim stopBtn = CreateModernButton("🛑 Parar Sistema", Color.FromArgb(239, 68, 68))
        stopBtn.Location = New Point(230, 60)
        AddHandler stopBtn.Click, AddressOf OnStopSystem
        parent.Controls.Add(stopBtn)

        Dim frontendBtn = CreateModernButton("🌐 Abrir Frontend", Color.FromArgb(59, 130, 246))
        frontendBtn.Location = New Point(20, 120)
        AddHandler frontendBtn.Click, AddressOf OnOpenFrontend
        parent.Controls.Add(frontendBtn)

        Dim swaggerBtn = CreateModernButton("📖 API Docs", Color.FromArgb(168, 85, 247))
        swaggerBtn.Location = New Point(230, 120)
        AddHandler swaggerBtn.Click, AddressOf OnOpenSwagger
        parent.Controls.Add(swaggerBtn)
    End Sub

    Private Sub CreateSyncButtons(parent As Panel)
        Dim initialBtn = CreateModernButton("📊 Carga Inicial", Color.FromArgb(234, 179, 8))
        initialBtn.Location = New Point(20, 60)
        AddHandler initialBtn.Click, AddressOf OnSyncInitial
        parent.Controls.Add(initialBtn)

        Dim incrementalBtn = CreateModernButton("⚡ Incremental", Color.FromArgb(250, 204, 21))
        incrementalBtn.Location = New Point(230, 60)
        AddHandler incrementalBtn.Click, AddressOf OnSyncIncremental
        parent.Controls.Add(incrementalBtn)

        Dim smartBtn = CreateModernButton("🧠 Sincronização Inteligente", Color.FromArgb(16, 185, 129))
        smartBtn.Location = New Point(75, 120)
        smartBtn.Size = New Size(280, 45)
        AddHandler smartBtn.Click, AddressOf OnSyncSmart
        parent.Controls.Add(smartBtn)
    End Sub

    Private Sub CreateMonitoringContent(parent As Panel)
        Dim portsBtn = CreateModernButton("🔍 Verificar Portas", Color.FromArgb(148, 163, 184))
        portsBtn.Location = New Point(20, 60)
        AddHandler portsBtn.Click, AddressOf OnCheckPorts
        parent.Controls.Add(portsBtn)

        Dim updateBtn = CreateModernButton("🔄 Atualizações", Color.FromArgb(99, 102, 241))
        updateBtn.Location = New Point(230, 60)
        AddHandler updateBtn.Click, AddressOf OnCheckUpdates
        parent.Controls.Add(updateBtn)

        Dim healthBtn = CreateModernButton("💚 Health Check", Color.FromArgb(20, 184, 166))
        healthBtn.Location = New Point(75, 120)
        AddHandler healthBtn.Click, AddressOf OnHealthCheck
        parent.Controls.Add(healthBtn)
    End Sub

    Private Sub CreateLogsViewer(parent As Panel)
        logsText = New RichTextBox()
        logsText.Location = New Point(20, 60)
        logsText.Size = New Size(400, 120)
        logsText.BackColor = Color.Black
        logsText.ForeColor = Color.FromArgb(34, 197, 94)
        logsText.Font = New Font("Consolas", 9.0!)
        logsText.ReadOnly = True
        logsText.BorderStyle = BorderStyle.None
        parent.Controls.Add(logsText)
    End Sub

    Private Function CreateModernButton(text As String, color As Color) As Button
        Dim btn As New Button()
        btn.Text = text
        btn.Size = New Size(180, 45)
        btn.BackColor = color
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        
        ' Hover effect
        AddHandler btn.MouseEnter, Sub()
                                        btn.BackColor = Color.FromArgb(Math.Min(255, color.R + 20), Math.Min(255, color.G + 20), Math.Min(255, color.B + 20))
                                    End Sub
        AddHandler btn.MouseLeave, Sub() btn.BackColor = color

        Return btn
    End Function

    Private Function CreateRoundedPath(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90)
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90)
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90)
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Sub PaintTopBarGradient(sender As Object, e As PaintEventArgs)
        Dim rect As New Rectangle(0, 0, topBar.Width, topBar.Height)
        Using brush As New LinearGradientBrush(rect, Color.FromArgb(99, 102, 241), Color.FromArgb(168, 85, 247), LinearGradientMode.Horizontal)
            e.Graphics.FillRectangle(brush, rect)
        End Using
    End Sub

    Private Sub PaintModernProgress(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        ' Background
        Using brush As New SolidBrush(Color.FromArgb(51, 65, 85))
            g.FillRectangle(brush, 0, 0, modernProgress.Width, modernProgress.Height)
        End Using

        ' Progress
        If progressValue > 0 Then
            Dim progressWidth = CInt((modernProgress.Width * progressValue) / 100)
            Using brush As New LinearGradientBrush(New Rectangle(0, 0, progressWidth, modernProgress.Height), Color.FromArgb(34, 197, 94), Color.FromArgb(59, 130, 246), LinearGradientMode.Horizontal)
                g.FillRectangle(brush, 0, 0, progressWidth, modernProgress.Height)
            End Using
        End If
    End Sub

    Private Sub SetupAnimations()
        animationTimer = New Timer()
        animationTimer.Interval = 50
        AddHandler animationTimer.Tick, AddressOf OnAnimationTick

        pulseTimer = New Timer()
        pulseTimer.Interval = 1000
        AddHandler pulseTimer.Tick, AddressOf OnPulseTick
        pulseTimer.Start()
    End Sub

    Private Sub OnAnimationTick(sender As Object, e As EventArgs)
        If progressValue < 100 Then
            progressValue += 2
            modernProgress.Invalidate()
        Else
            animationTimer.Stop()
            progressValue = 0
            SetStatus("Operação concluída")
        End If
    End Sub

    Private Sub OnPulseTick(sender As Object, e As EventArgs)
        ' Pulse effect for active status cards
        UpdateStatusCards()
    End Sub

    Private Sub SetStatus(text As String)
        operationStatus.Text = text
    End Sub

    Private Sub StartProgress(text As String)
        SetStatus(text)
        progressValue = 0
        animationTimer.Start()
    End Sub

    Private Sub UpdateStatusCards()
        ' Update backend status
        Dim backendStatus = If(backendApiManager.IsRunning, "Rodando", "Parado")
        Dim backendColor = If(backendApiManager.IsRunning, Color.FromArgb(34, 197, 94), Color.FromArgb(239, 68, 68))
        UpdateStatusCard(backendStatusCard, "Backend", backendStatus, backendColor)

        ' Update frontend status
        Dim frontendStatus = If(frontendServer.IsServerRunning, "Rodando", "Parado")
        Dim frontendColor = If(frontendServer.IsServerRunning, Color.FromArgb(34, 197, 94), Color.FromArgb(239, 68, 68))
        UpdateStatusCard(frontendStatusCard, "Frontend", frontendStatus, frontendColor)

        ' Update database status
        Try
            Dim dbOk = databaseManager.VerificarExistenciaBanco()
            Dim dbStatus = If(dbOk, "Conectado", "Erro")
            Dim dbColor = If(dbOk, Color.FromArgb(34, 197, 94), Color.FromArgb(239, 68, 68))
            UpdateStatusCard(dbStatusCard, "Database", dbStatus, dbColor)
        Catch
            UpdateStatusCard(dbStatusCard, "Database", "Erro", Color.FromArgb(239, 68, 68))
        End Try
    End Sub

    Private Sub UpdateStatusCard(card As Panel, title As String, status As String, color As Color)
        card.Invalidate()
    End Sub

    ' Event handlers
    Private Sub OnFormLoad(sender As Object, e As EventArgs)
        previousConsoleOut = Console.Out
        consoleWriter = New ModernConsoleWriter(logsText)
        Console.SetOut(consoleWriter)
        SetStatus("Dashboard carregado - Sistema pronto")
    End Sub

    Private Sub OnFormClosed(sender As Object, e As FormClosedEventArgs)
        Try
            If previousConsoleOut IsNot Nothing Then
                Console.SetOut(previousConsoleOut)
            End If
        Catch
        End Try
    End Sub

    Private Sub OnProgress(p As Integer, s As String)
        LogMessage(s)
        progressValue = p
        modernProgress.Invalidate()
        SetStatus(s)
    End Sub

    Private Sub OnPortStatus(port As Integer, status As String)
        LogMessage(status)
    End Sub

    Private Sub OnBackendStatus(message As String)
        LogMessage($"[Backend] {message}")
    End Sub

    Private Sub OnBackendStarted(url As String)
        LogMessage($"✅ Backend iniciado: {url}")
        UpdateStatusCards()
    End Sub

    Private Sub OnBackendStopped()
        LogMessage("🛑 Backend parado")
        UpdateStatusCards()
    End Sub

    Private Sub OnFrontendStatus(message As String)
        LogMessage($"[Frontend] {message}")
    End Sub

    Private Sub OnFrontendStarted(url As String)
        LogMessage($"✅ Frontend iniciado: {url}")
        UpdateStatusCards()
    End Sub

    Private Sub OnFrontendStopped()
        LogMessage("🛑 Frontend parado")
        UpdateStatusCards()
    End Sub

    ' Action handlers
    Private Async Sub OnStartSystem(sender As Object, e As EventArgs)
        Try
            StartProgress("Iniciando sistema completo...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(1000)
            Dim okBackend = Await backendApiManager.StartAsync()
            Dim okFrontend = frontendServer.StartAsync()
            If okBackend AndAlso okFrontend Then
                LogMessage("🚀 Sistema iniciado com sucesso!")
            Else
                LogMessage("❌ Falha ao iniciar alguns componentes")
            End If
        Catch ex As Exception
            LogMessage($"❌ Erro: {ex.Message}")
        End Try
    End Sub

    Private Sub OnStopSystem(sender As Object, e As EventArgs)
        Try
            frontendServer.[Stop]()
            backendApiManager.[Stop]()
            LogMessage("🛑 Sistema parado")
        Catch ex As Exception
            LogMessage($"❌ Erro ao parar: {ex.Message}")
        End Try
    End Sub

    Private Sub OnOpenFrontend(sender As Object, e As EventArgs)
        Try
            If frontendServer.IsServerRunning Then
                Process.Start(frontendServer.ServerUrl)
                LogMessage("🌐 Abrindo frontend no navegador")
            Else
                MessageBox.Show("Frontend não está rodando", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            LogMessage($"❌ Erro ao abrir frontend: {ex.Message}")
        End Try
    End Sub

    Private Sub OnOpenSwagger(sender As Object, e As EventArgs)
        Try
            If backendApiManager.IsRunning Then
                Process.Start(backendApiManager.SwaggerUrl)
                LogMessage("📖 Abrindo documentação da API")
            Else
                MessageBox.Show("Backend não está rodando", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            LogMessage($"❌ Erro ao abrir Swagger: {ex.Message}")
        End Try
    End Sub

    Private Sub OnSyncInitial(sender As Object, e As EventArgs)
        StartProgress("Executando carga inicial...")
        Task.Run(Sub()
                     Try
                         sincronizador.RealizarCargaInicial()
                         Me.Invoke(Sub() LogMessage("📊 Carga inicial concluída"))
                     Catch ex As Exception
                         Me.Invoke(Sub() LogMessage($"❌ Erro na carga inicial: {ex.Message}"))
                     End Try
                 End Sub)
    End Sub

    Private Sub OnSyncIncremental(sender As Object, e As EventArgs)
        StartProgress("Executando carga incremental...")
        Task.Run(Sub()
                     Try
                         sincronizador.RealizarCargaIncremental()
                         Me.Invoke(Sub() LogMessage("⚡ Carga incremental concluída"))
                     Catch ex As Exception
                         Me.Invoke(Sub() LogMessage($"❌ Erro na carga incremental: {ex.Message}"))
                     End Try
                 End Sub)
    End Sub

    Private Sub OnSyncSmart(sender As Object, e As EventArgs)
        StartProgress("Executando sincronização inteligente...")
        Task.Run(Sub()
                     Try
                         sincronizador.ExecutarSincronizacaoInteligente()
                         Me.Invoke(Sub() LogMessage("🧠 Sincronização inteligente concluída"))
                     Catch ex As Exception
                         Me.Invoke(Sub() LogMessage($"❌ Erro na sincronização: {ex.Message}"))
                     End Try
                 End Sub)
    End Sub

    Private Sub OnCheckPorts(sender As Object, e As EventArgs)
        StartProgress("Verificando portas...")
        Task.Run(Sub()
                     Try
                         portManager.FreeAllSystemPorts()
                         Me.Invoke(Sub() LogMessage("🔍 Verificação de portas concluída"))
                     Catch ex As Exception
                         Me.Invoke(Sub() LogMessage($"❌ Erro na verificação: {ex.Message}"))
                     End Try
                 End Sub)
    End Sub

    Private Async Sub OnCheckUpdates(sender As Object, e As EventArgs)
        Try
            StartProgress("Verificando atualizações...")
            Dim result = Await updateManager.VerificarAtualizacoes()
            If result.Success Then
                If result.HasUpdate Then
                    LogMessage($"🔄 Nova versão disponível: {result.VersionInfo.Version}")
                Else
                    LogMessage("✅ Sistema está atualizado")
                End If
            Else
                LogMessage($"❌ Erro na verificação: {result.Message}")
            End If
        Catch ex As Exception
            LogMessage($"❌ Erro: {ex.Message}")
        End Try
    End Sub

    Private Sub OnHealthCheck(sender As Object, e As EventArgs)
        Try
            backendApiManager.CheckHealthAsync()
            LogMessage("💚 Executando health check...")
        Catch ex As Exception
            LogMessage($"❌ Erro no health check: {ex.Message}")
        End Try
    End Sub

    Private Sub LogMessage(message As String)
        Try
            If logsText IsNot Nothing AndAlso Not logsText.IsDisposed Then
                If logsText.InvokeRequired Then
                    logsText.Invoke(Sub() LogMessage(message))
                Else
                    logsText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}")
                    logsText.SelectionStart = logsText.TextLength
                    logsText.ScrollToCaret()
                End If
            End If
        Catch
        End Try
    End Sub
End Class

' Modern console writer for beautiful logs
Public Class ModernConsoleWriter
    Inherits TextWriter

    Private ReadOnly _logs As RichTextBox

    Public Sub New(logs As RichTextBox)
        _logs = logs
    End Sub

    Public Overrides ReadOnly Property Encoding As System.Text.Encoding
        Get
            Return System.Text.Encoding.UTF8
        End Get
    End Property

    Public Overrides Sub Write(value As String)
        Try
            If _logs IsNot Nothing AndAlso Not _logs.IsDisposed Then
                If _logs.InvokeRequired Then
                    _logs.Invoke(Sub() WriteToLogs(value))
                Else
                    WriteToLogs(value)
                End If
            End If
        Catch
        End Try
    End Sub

    Public Overrides Sub WriteLine(value As String)
        Write(value & Environment.NewLine)
    End Sub

    Private Sub WriteToLogs(value As String)
        Try
            _logs.AppendText(value)
            _logs.SelectionStart = _logs.TextLength
            _logs.ScrollToCaret()
            Application.DoEvents()
        Catch
        End Try
    End Sub
End Class

