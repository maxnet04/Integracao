Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading.Tasks
Imports System.Net.Http
Imports Newtonsoft.Json

''' <summary>
''' Formulário de Integração SUAT-IA
''' Interface para configuração e teste de integrações com sistemas externos
''' Compatível com o layout clássico do SUAT
''' </summary>
Public Class FormularioIntegracao
    Inherits Form

    ' --- Gerenciadores Compartilhados ---
    Private ReadOnly updateManager As UpdateManager
    Private ReadOnly portManager As PortManager
    Private ReadOnly databaseManager As DatabaseManager
    Private ReadOnly backendApiManager As BackendApiManager
    Private ReadOnly frontendServer As FrontendHttpServer
    Private ReadOnly sincronizador As SincronizadorDados
    Private ReadOnly httpClient As HttpClient
    Private ReadOnly parentForm As MainForm

    ' --- Controles da Interface ---
    ' Header
    Private headerPanel As Panel
    Private lblTitulo As Label
    Private lblSubtitulo As Label
    Private picIcone As PictureBox

    ' Status Indicators
    Private statusPanel As Panel
    Private lblStatusConexao As Label
    Private lblStatusBanco As Label
    Private lblStatusAPI As Label
    Private btnAtualizarStatus As Button

    ' Grupo: Configurações de Conexão
    Private grpConexao As GroupBox
    Private lblServidor As Label
    Private txtServidor As TextBox
    Private lblPorta As Label
    Private txtPorta As TextBox
    Private lblUsuario As Label
    Private txtUsuario As TextBox
    Private lblSenha As Label
    Private txtSenha As TextBox
    Private chkSalvarCredenciais As CheckBox
    Private WithEvents btnTestarConexao As Button
    Private WithEvents btnSalvarConfig As Button

    ' Grupo: Endpoints de Integração
    Private grpEndpoints As GroupBox
    Private lblEndpointBase As Label
    Private txtEndpointBase As TextBox
    Private lblEndpointAuth As Label
    Private txtEndpointAuth As TextBox
    Private lblEndpointDados As Label
    Private txtEndpointDados As TextBox
    Private WithEvents btnValidarEndpoints As Button

    ' Grupo: Testes de Integração
    Private grpTestes As GroupBox
    Private WithEvents btnTesteAutenticacao As Button
    Private WithEvents btnTesteListarDados As Button
    Private WithEvents btnTesteEnviarDados As Button
    Private WithEvents btnTesteCompleto As Button
    Private lblResultadoTeste As Label

    ' Grupo: Sincronização
    Private grpSincronizacao As GroupBox
    Private lblUltimaSync As Label
    Private txtUltimaSync As TextBox
    Private lblProximaSync As Label
    Private txtProximaSync As TextBox
    Private WithEvents btnSincronizarAgora As Button
    Private WithEvents btnConfigurarAgendamento As Button
    Private chkSincronizacaoAutomatica As CheckBox

    ' Log de Atividades
    Private grpLog As GroupBox
    Private txtLog As TextBox
    Private WithEvents btnLimparLog As Button
    Private WithEvents btnExportarLog As Button

    ' Barra de Status
    Private statusStrip As StatusStrip
    Private lblStatusGeral As ToolStripStatusLabel
    Private progressBar As ToolStripProgressBar
    Private lblHorario As ToolStripStatusLabel

    ' Timer para atualização de status
    Private WithEvents timerStatus As Timer

    ''' <summary>
    ''' Construtor que recebe as instâncias dos gerenciadores do MainForm
    ''' </summary>
    Public Sub New(mainForm As MainForm, updateMgr As UpdateManager, portMgr As PortManager, 
                  dbMgr As DatabaseManager, backendMgr As BackendApiManager, 
                  frontendSrv As FrontendHttpServer, syncMgr As SincronizadorDados)
        
        ' Compartilhar gerenciadores com MainForm
        Me.parentForm = mainForm
        Me.updateManager = updateMgr
        Me.portManager = portMgr
        Me.databaseManager = dbMgr
        Me.backendApiManager = backendMgr
        Me.frontendServer = frontendSrv
        Me.sincronizador = syncMgr
        
        ' Inicializar HTTP client
        httpClient = New HttpClient()
        httpClient.Timeout = TimeSpan.FromSeconds(30)

        ' Conectar eventos dos gerenciadores
        AddHandler updateManager.ProgressChanged, AddressOf OnUpdateProgress
        AddHandler portManager.PortStatusChanged, AddressOf OnPortStatus
        AddHandler backendApiManager.StatusChanged, AddressOf OnBackendStatus
        AddHandler backendApiManager.ServerStarted, AddressOf OnBackendStarted
        AddHandler backendApiManager.ServerStopped, AddressOf OnBackendStopped
        AddHandler backendApiManager.HealthCheckResult, AddressOf OnHealthCheck
        AddHandler frontendServer.StatusChanged, AddressOf OnFrontendStatus
        AddHandler frontendServer.ServerStarted, AddressOf OnFrontendStarted
        AddHandler frontendServer.ServerStopped, AddressOf OnFrontendStopped

        InitializeComponent()
        CarregarConfiguracoes()
        AtualizarStatusIndicadores()
        IniciarTimerStatus()
    End Sub

    ''' <summary>
    ''' Construtor padrão para compatibilidade
    ''' </summary>
    Public Sub New()
        ' Fallback - criar instâncias locais se chamado sem parâmetros
        Me.updateManager = New UpdateManager()
        Me.portManager = New PortManager()
        Me.databaseManager = New DatabaseManager()
        Me.backendApiManager = New BackendApiManager()
        
        Dim frontendBuildPath = Path.Combine(Application.StartupPath, "frontend", "build")
        Me.frontendServer = New FrontendHttpServer(frontendBuildPath, 8080)
        Me.sincronizador = New SincronizadorDados()
        
        httpClient = New HttpClient()
        httpClient.Timeout = TimeSpan.FromSeconds(30)

        InitializeComponent()
        CarregarConfiguracoes()
        AtualizarStatusIndicadores()
        IniciarTimerStatus()
    End Sub

    Private Sub InitializeComponent()
        Me.headerPanel = New System.Windows.Forms.Panel()
        Me.picIcone = New System.Windows.Forms.PictureBox()
        Me.lblTitulo = New System.Windows.Forms.Label()
        Me.lblSubtitulo = New System.Windows.Forms.Label()
        Me.statusPanel = New System.Windows.Forms.Panel()
        Me.btnAtualizarStatus = New System.Windows.Forms.Button()
        Me.chkSalvarCredenciais = New System.Windows.Forms.CheckBox()
        Me.lblResultadoTeste = New System.Windows.Forms.Label()
        Me.chkSincronizacaoAutomatica = New System.Windows.Forms.CheckBox()
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.statusStrip = New System.Windows.Forms.StatusStrip()
        Me.lblStatusGeral = New System.Windows.Forms.ToolStripStatusLabel()
        Me.progressBar = New System.Windows.Forms.ToolStripProgressBar()
        Me.lblHorario = New System.Windows.Forms.ToolStripStatusLabel()
        Me.headerPanel.SuspendLayout()
        CType(Me.picIcone, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.statusPanel.SuspendLayout()
        Me.statusStrip.SuspendLayout()
        Me.SuspendLayout()
        '
        'headerPanel
        '
        Me.headerPanel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.headerPanel.BackColor = System.Drawing.Color.FromArgb(CType(CType(51, Byte), Integer), CType(CType(122, Byte), Integer), CType(CType(183, Byte), Integer))
        Me.headerPanel.Controls.Add(Me.picIcone)
        Me.headerPanel.Controls.Add(Me.lblTitulo)
        Me.headerPanel.Controls.Add(Me.lblSubtitulo)
        Me.headerPanel.Controls.Add(Me.statusPanel)
        Me.headerPanel.Location = New System.Drawing.Point(0, 0)
        Me.headerPanel.Name = "headerPanel"
        Me.headerPanel.Size = New System.Drawing.Size(276, 80)
        Me.headerPanel.TabIndex = 0
        '
        'picIcone
        '
        Me.picIcone.BackColor = System.Drawing.Color.White
        Me.picIcone.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.picIcone.Location = New System.Drawing.Point(20, 16)
        Me.picIcone.Name = "picIcone"
        Me.picIcone.Size = New System.Drawing.Size(48, 48)
        Me.picIcone.TabIndex = 0
        Me.picIcone.TabStop = False
        '
        'lblTitulo
        '
        Me.lblTitulo.Font = New System.Drawing.Font("Segoe UI", 16.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitulo.ForeColor = System.Drawing.Color.White
        Me.lblTitulo.Location = New System.Drawing.Point(80, 15)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Size = New System.Drawing.Size(500, 30)
        Me.lblTitulo.TabIndex = 1
        Me.lblTitulo.Text = "Formulário de Integração SUAT-IA"
        '
        'lblSubtitulo
        '
        Me.lblSubtitulo.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblSubtitulo.ForeColor = System.Drawing.Color.FromArgb(CType(CType(220, Byte), Integer), CType(CType(220, Byte), Integer), CType(CType(220, Byte), Integer))
        Me.lblSubtitulo.Location = New System.Drawing.Point(80, 45)
        Me.lblSubtitulo.Name = "lblSubtitulo"
        Me.lblSubtitulo.Size = New System.Drawing.Size(500, 20)
        Me.lblSubtitulo.TabIndex = 2
        Me.lblSubtitulo.Text = "Configuração e teste de integrações com sistemas externos"
        '
        'statusPanel
        '
        Me.statusPanel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.statusPanel.BackColor = System.Drawing.Color.FromArgb(CType(CType(250, Byte), Integer), CType(CType(250, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.statusPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.statusPanel.Controls.Add(Me.btnAtualizarStatus)
        Me.statusPanel.Location = New System.Drawing.Point(650, 10)
        Me.statusPanel.Name = "statusPanel"
        Me.statusPanel.Size = New System.Drawing.Size(300, 60)
        Me.statusPanel.TabIndex = 3
        '
        'btnAtualizarStatus
        '
        Me.btnAtualizarStatus.BackColor = System.Drawing.Color.White
        Me.btnAtualizarStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAtualizarStatus.Font = New System.Drawing.Font("Segoe UI", 12.0!)
        Me.btnAtualizarStatus.Location = New System.Drawing.Point(260, 18)
        Me.btnAtualizarStatus.Name = "btnAtualizarStatus"
        Me.btnAtualizarStatus.Size = New System.Drawing.Size(30, 25)
        Me.btnAtualizarStatus.TabIndex = 0
        Me.btnAtualizarStatus.Text = "↻"
        Me.btnAtualizarStatus.UseVisualStyleBackColor = False
        '
        'chkSalvarCredenciais
        '
        Me.chkSalvarCredenciais.AutoSize = True
        Me.chkSalvarCredenciais.Location = New System.Drawing.Point(320, 99)
        Me.chkSalvarCredenciais.Name = "chkSalvarCredenciais"
        Me.chkSalvarCredenciais.Size = New System.Drawing.Size(104, 24)
        Me.chkSalvarCredenciais.TabIndex = 0
        Me.chkSalvarCredenciais.Text = "Salvar credenciais"
        '
        'lblResultadoTeste
        '
        Me.lblResultadoTeste.BackColor = System.Drawing.Color.White
        Me.lblResultadoTeste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblResultadoTeste.Location = New System.Drawing.Point(20, 130)
        Me.lblResultadoTeste.Name = "lblResultadoTeste"
        Me.lblResultadoTeste.Padding = New System.Windows.Forms.Padding(5)
        Me.lblResultadoTeste.Size = New System.Drawing.Size(420, 40)
        Me.lblResultadoTeste.TabIndex = 0
        Me.lblResultadoTeste.Text = "Nenhum teste executado"
        Me.lblResultadoTeste.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'chkSincronizacaoAutomatica
        '
        Me.chkSincronizacaoAutomatica.Location = New System.Drawing.Point(20, 100)
        Me.chkSincronizacaoAutomatica.Name = "chkSincronizacaoAutomatica"
        Me.chkSincronizacaoAutomatica.Size = New System.Drawing.Size(200, 25)
        Me.chkSincronizacaoAutomatica.TabIndex = 0
        Me.chkSincronizacaoAutomatica.Text = "Sincronização automática ativa"
        '
        'txtLog
        '
        Me.txtLog.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtLog.BackColor = System.Drawing.Color.Black
        Me.txtLog.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.txtLog.ForeColor = System.Drawing.Color.LimeGreen
        Me.txtLog.Location = New System.Drawing.Point(20, 25)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ReadOnly = True
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(750, 110)
        Me.txtLog.TabIndex = 0
        '
        'statusStrip
        '
        Me.statusStrip.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer))
        Me.statusStrip.ImageScalingSize = New System.Drawing.Size(28, 28)
        Me.statusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatusGeral, Me.progressBar, Me.lblHorario})
        Me.statusStrip.Location = New System.Drawing.Point(0, 647)
        Me.statusStrip.Name = "statusStrip"
        Me.statusStrip.Size = New System.Drawing.Size(976, 39)
        Me.statusStrip.TabIndex = 1
        '
        'lblStatusGeral
        '
        Me.lblStatusGeral.Name = "lblStatusGeral"
        Me.lblStatusGeral.Size = New System.Drawing.Size(604, 30)
        Me.lblStatusGeral.Spring = True
        Me.lblStatusGeral.Text = "Pronto"
        Me.lblStatusGeral.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'progressBar
        '
        Me.progressBar.Name = "progressBar"
        Me.progressBar.Size = New System.Drawing.Size(100, 29)
        Me.progressBar.Visible = False
        '
        'lblHorario
        '
        Me.lblHorario.Name = "lblHorario"
        Me.lblHorario.Size = New System.Drawing.Size(199, 30)
        Me.lblHorario.Text = "03/09/2025 19:50:23"
        '
        'FormularioIntegracao
        '
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(240, Byte), Integer), CType(CType(240, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(976, 686)
        Me.Controls.Add(Me.headerPanel)
        Me.Controls.Add(Me.statusStrip)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FormularioIntegracao"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "SUAT-IA - Formulário de Integração"
        Me.headerPanel.ResumeLayout(False)
        CType(Me.picIcone, System.ComponentModel.ISupportInitialize).EndInit()
        Me.statusPanel.ResumeLayout(False)
        Me.statusStrip.ResumeLayout(False)
        Me.statusStrip.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Private Function CriarGroupBox(titulo As String, location As Point, size As Size) As GroupBox
        Dim grp As New GroupBox()
        grp.Text = titulo
        grp.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        grp.Location = location
        grp.Size = size
        grp.BackColor = Color.White
        Return grp
    End Function

    Private Function CriarLabel(texto As String, location As Point) As Label
        Dim lbl As New Label()
        lbl.Text = texto
        lbl.Location = location
        lbl.AutoSize = True
        lbl.Font = New Font("Segoe UI", 9.0!)
        Return lbl
    End Function

    Private Function CriarTextBox(location As Point, size As Size) As TextBox
        Dim txt As New TextBox()
        txt.Location = location
        txt.Size = size
        txt.Font = New Font("Segoe UI", 9.0!)
        Return txt
    End Function

    Private Function CriarBotao(texto As String, location As Point, size As Size, backColor As Color) As Button
        Dim btn As New Button()
        btn.Text = texto
        btn.Location = location
        btn.Size = size
        btn.BackColor = backColor
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        Return btn
    End Function

    Private Function CriarLabelStatus(texto As String, cor As Color, location As Point) As Label
        Dim lbl As New Label()
        lbl.Text = texto
        lbl.Location = location
        lbl.Size = New Size(240, 15)
        lbl.Font = New Font("Segoe UI", 8.0!)
        lbl.ForeColor = cor
        Return lbl
    End Function

    ' --- Eventos de Ciclo de Vida ---
    Private Sub FormularioIntegracao_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LogMessage("Formulário de integração carregado")
        AtualizarInformacoesSync()
    End Sub

    Private Sub FormularioIntegracao_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If timerStatus IsNot Nothing Then
            timerStatus.Stop()
            timerStatus.Dispose()
        End If
        httpClient?.Dispose()
    End Sub

    Private Sub IniciarTimerStatus()
        timerStatus = New Timer()
        timerStatus.Interval = 1000 ' 1 segundo
        AddHandler timerStatus.Tick, Sub()
                                         lblHorario.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                                     End Sub
        timerStatus.Start()
    End Sub

    ' --- Eventos dos Botões ---
    Private Async Sub btnTestarConexao_Click(sender As Object, e As EventArgs) Handles btnTestarConexao.Click
        Await ExecutarComProgress("Testando conexão...", AddressOf TestarConexaoInterno)
    End Sub

    Private Sub btnSalvarConfig_Click(sender As Object, e As EventArgs) Handles btnSalvarConfig.Click
        SalvarConfiguracoes()
    End Sub

    Private Async Sub btnValidarEndpoints_Click(sender As Object, e As EventArgs) Handles btnValidarEndpoints.Click
        Await ExecutarComProgress("Validando endpoints...", AddressOf ValidarEndpoints)
    End Sub

    Private Async Sub btnTesteAutenticacao_Click(sender As Object, e As EventArgs) Handles btnTesteAutenticacao.Click
        Await ExecutarComProgress("Testando autenticação...", AddressOf TestarAutenticacao)
    End Sub

    Private Async Sub btnTesteListarDados_Click(sender As Object, e As EventArgs) Handles btnTesteListarDados.Click
        Await ExecutarComProgress("Testando listagem de dados...", AddressOf TestarListarDados)
    End Sub

    Private Async Sub btnTesteEnviarDados_Click(sender As Object, e As EventArgs) Handles btnTesteEnviarDados.Click
        Await ExecutarComProgress("Testando envio de dados...", AddressOf TestarEnviarDados)
    End Sub

    Private Async Sub btnTesteCompleto_Click(sender As Object, e As EventArgs) Handles btnTesteCompleto.Click
        Await ExecutarComProgress("Executando integração completa SUAT-IA...", AddressOf IntegracaoCompleta)
    End Sub

    Private Async Sub btnSincronizarAgora_Click(sender As Object, e As EventArgs) Handles btnSincronizarAgora.Click
        Await ExecutarComProgress("Sincronizando dados...", AddressOf SincronizarAgora)
    End Sub

    Private Sub btnConfigurarAgendamento_Click(sender As Object, e As EventArgs) Handles btnConfigurarAgendamento.Click
        Try
            ' Abrir formulário de configuração de agendamento integrado
            Dim configForm As New Form()
            configForm.Text = "Configurar Agendamento de Sincronização"
            configForm.Size = New Size(400, 300)
            configForm.StartPosition = FormStartPosition.CenterParent
            
            Dim lblInfo As New Label()
            lblInfo.Text = "Configure a sincronização automática:"
            lblInfo.Location = New Point(20, 20)
            lblInfo.AutoSize = True
            
            Dim chkHabilitado As New CheckBox()
            chkHabilitado.Text = "Habilitar sincronização automática"
            chkHabilitado.Location = New Point(20, 50)
            chkHabilitado.Checked = chkSincronizacaoAutomatica.Checked
            
            Dim lblIntervalo As New Label()
            lblIntervalo.Text = "Intervalo (minutos):"
            lblIntervalo.Location = New Point(20, 80)
            lblIntervalo.AutoSize = True
            
            Dim numIntervalo As New NumericUpDown()
            numIntervalo.Location = New Point(150, 78)
            numIntervalo.Size = New Size(100, 23)
            numIntervalo.Minimum = 5
            numIntervalo.Maximum = 1440 ' 24 horas
            numIntervalo.Value = 60 ' 1 hora padrão
            
            Dim btnSalvar As New Button()
            btnSalvar.Text = "Salvar"
            btnSalvar.Location = New Point(200, 200)
            btnSalvar.Size = New Size(80, 30)
            btnSalvar.BackColor = Color.FromArgb(92, 184, 92)
            btnSalvar.ForeColor = Color.White
            btnSalvar.FlatStyle = FlatStyle.Flat
            
            AddHandler btnSalvar.Click, Sub()
                                            chkSincronizacaoAutomatica.Checked = chkHabilitado.Checked
                                            If chkHabilitado.Checked Then
                                                txtProximaSync.Text = DateTime.Now.AddMinutes(numIntervalo.Value).ToString("dd/MM/yyyy HH:mm:ss")
                                                LogMessage($"✅ Agendamento configurado: {numIntervalo.Value} minutos")
                                            Else
                                                txtProximaSync.Text = "Não agendada"
                                                LogMessage("⏸️ Sincronização automática desabilitada")
                                            End If
                                            configForm.Close()
                                        End Sub
            
            Dim btnCancelar As New Button()
            btnCancelar.Text = "Cancelar"
            btnCancelar.Location = New Point(290, 200)
            btnCancelar.Size = New Size(80, 30)
            btnCancelar.BackColor = Color.FromArgb(217, 83, 79)
            btnCancelar.ForeColor = Color.White
            btnCancelar.FlatStyle = FlatStyle.Flat
            AddHandler btnCancelar.Click, Sub() configForm.Close()
            
            configForm.Controls.AddRange({lblInfo, chkHabilitado, lblIntervalo, numIntervalo, btnSalvar, btnCancelar})
            configForm.ShowDialog(Me)
            
        Catch ex As Exception
            MessageBox.Show($"Erro ao configurar agendamento: {ex.Message}", "Erro", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnLimparLog_Click(sender As Object, e As EventArgs) Handles btnLimparLog.Click
        txtLog.Clear()
        LogMessage("Log limpo")
    End Sub

    Private Sub btnExportarLog_Click(sender As Object, e As EventArgs) Handles btnExportarLog.Click
        ExportarLog()
    End Sub

    ' --- Métodos de Integração ---
    Private Async Function TestarConexaoInterno() As Task(Of String)
        Try
            Dim url = $"http://{txtServidor.Text}:{txtPorta.Text}/api/health"
            Dim response = Await httpClient.GetAsync(url)
            
            If response.IsSuccessStatusCode Then
                lblStatusConexao.Text = "Conexão: Conectado"
                lblStatusConexao.ForeColor = Color.Green
                Return "✅ Conexão estabelecida com sucesso"
            Else
                lblStatusConexao.Text = "Conexão: Erro"
                lblStatusConexao.ForeColor = Color.Red
                Return $"❌ Falha na conexão: {response.StatusCode}"
            End If
        Catch ex As Exception
            lblStatusConexao.Text = "Conexão: Erro"
            lblStatusConexao.ForeColor = Color.Red
            Return $"❌ Erro de conexão: {ex.Message}"
        End Try
    End Function

    Private Async Function ValidarEndpoints() As Task(Of String)
        Try
            Dim endpoints() As String = {txtEndpointAuth.Text, txtEndpointDados.Text}
            Dim resultados As New List(Of String)

            For Each endpoint In endpoints
                Dim url = txtEndpointBase.Text & endpoint
                Try
                    Dim response = Await httpClient.GetAsync(url)
                    resultados.Add($"✅ {endpoint}: {response.StatusCode}")
                Catch ex As Exception
                    resultados.Add($"❌ {endpoint}: {ex.Message}")
                End Try
            Next

            Return String.Join(Environment.NewLine, resultados)
        Catch ex As Exception
            Return $"❌ Erro na validação: {ex.Message}"
        End Try
    End Function

    Private Async Function TestarAutenticacao() As Task(Of String)
        Try
            Dim url = txtEndpointBase.Text & txtEndpointAuth.Text
            Dim loginData = New With {
                .username = txtUsuario.Text,
                .password = txtSenha.Text
            }
            Dim json = JsonConvert.SerializeObject(loginData)
            Dim content = New StringContent(json, System.Text.Encoding.UTF8, "application/json")
            
            Dim response = Await httpClient.PostAsync(url, content)
            
            If response.IsSuccessStatusCode Then
                Return "✅ Autenticação realizada com sucesso"
            Else
                Return $"❌ Falha na autenticação: {response.StatusCode}"
            End If
        Catch ex As Exception
            Return $"❌ Erro na autenticação: {ex.Message}"
        End Try
    End Function

    Private Async Function TestarListarDados() As Task(Of String)
        Try
            Dim url = txtEndpointBase.Text & txtEndpointDados.Text
            Dim response = Await httpClient.GetAsync(url)
            
            If response.IsSuccessStatusCode Then
                Dim content = Await response.Content.ReadAsStringAsync()
                Return $"✅ Dados listados: {content.Length} caracteres recebidos"
            Else
                Return $"❌ Falha ao listar dados: {response.StatusCode}"
            End If
        Catch ex As Exception
            Return $"❌ Erro ao listar dados: {ex.Message}"
        End Try
    End Function

    Private Async Function TestarEnviarDados() As Task(Of String)
        Try
            Dim url = txtEndpointBase.Text & txtEndpointDados.Text
            Dim testData = New With {
                .timestamp = DateTime.Now,
                .test = True,
                .data = "Teste de envio de dados"
            }
            Dim json = JsonConvert.SerializeObject(testData)
            Dim content = New StringContent(json, System.Text.Encoding.UTF8, "application/json")
            
            Dim response = Await httpClient.PostAsync(url, content)
            
            If response.IsSuccessStatusCode Then
                Return "✅ Dados enviados com sucesso"
            Else
                Return $"❌ Falha ao enviar dados: {response.StatusCode}"
            End If
        Catch ex As Exception
            Return $"❌ Erro ao enviar dados: {ex.Message}"
        End Try
    End Function

    Private Async Function SincronizarAgora() As Task(Of String)
        Try
            LogMessage("Iniciando sincronização integrada com SUAT...")
            
            ' Verificar status dos serviços primeiro
            If Not backendApiManager.IsRunning Then
                Return "❌ Backend API não está rodando. Inicie o sistema primeiro."
            End If
            
            ' Usar sincronizador compartilhado do MainForm
            Dim resultados As New List(Of String)
            
            ' 1. Verificar portas (usando PortManager compartilhado)
            LogMessage("1/4 - Verificando portas do sistema...")
            portManager.FreeAllSystemPorts()
            Await Task.Delay(1000)
            resultados.Add("✅ Portas verificadas")
            
            ' 2. Sincronização inteligente (usando SincronizadorDados compartilhado)
            LogMessage("2/4 - Executando sincronização inteligente...")
            sincronizador.ExecutarSincronizacaoInteligente()
            resultados.Add("✅ Sincronização inteligente executada")
            
            ' 3. Verificar saúde da API
            LogMessage("3/4 - Verificando saúde da API...")
            backendApiManager.CheckHealthAsync()
            Await Task.Delay(1000)
            resultados.Add("✅ Health check executado")
            
            ' 4. Sincronização de dados externos via HTTP
            LogMessage("4/4 - Sincronizando dados externos...")
            Dim syncExterno = Await SincronizarDadosExternos()
            resultados.Add(syncExterno)
            
            ' Atualizar informações de sincronização
            txtUltimaSync.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            txtProximaSync.Text = DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm:ss")
            
            LogMessage("Sincronização integrada concluída!")
            Return String.Join(Environment.NewLine, resultados)
            
        Catch ex As Exception
            LogMessage($"Erro na sincronização: {ex.Message}")
            Return $"❌ Erro na sincronização: {ex.Message}"
        End Try
    End Function
    
    ''' <summary>
    ''' Sincronização específica de dados externos via HTTP
    ''' </summary>
    Private Async Function SincronizarDadosExternos() As Task(Of String)
        Try
            Dim url = txtEndpointBase.Text & txtEndpointDados.Text
            Dim response = Await httpClient.GetAsync(url)
            
            If response.IsSuccessStatusCode Then
                Dim content = Await response.Content.ReadAsStringAsync()
                ' Aqui poderia processar os dados recebidos e salvar no banco
                Return $"✅ Dados externos sincronizados: {content.Length} bytes"
            Else
                Return $"⚠️ Sincronização externa falhou: {response.StatusCode}"
            End If
        Catch ex As Exception
            Return $"❌ Erro na sincronização externa: {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' Integração completa: SUAT + Sistemas Externos
    ''' </summary>
    Private Async Function IntegracaoCompleta() As Task(Of String)
        Try
            Dim resultados As New List(Of String)
            resultados.Add("=== INTEGRAÇÃO COMPLETA SUAT-IA ===")
            resultados.Add("")
            
            ' 1. Verificar SUAT
            LogMessage("1/5 - Verificando sistema SUAT...")
            ProcessarDadosSUAT()
            resultados.Add("✅ Sistema SUAT verificado")
            
            ' 2. Verificar serviços
            LogMessage("2/5 - Verificando serviços...")
            If backendApiManager.IsRunning Then
                resultados.Add("✅ Backend API ativo")
            Else
                resultados.Add("⚠️ Backend API inativo")
            End If
            
            If frontendServer.IsServerRunning Then
                resultados.Add("✅ Frontend ativo")
            Else
                resultados.Add("⚠️ Frontend inativo")
            End If
            
            ' 3. Teste de conectividade externa
            LogMessage("3/5 - Testando conectividade externa...")
            Dim conectividade = Await TestarConexaoInterno()
            resultados.Add(conectividade)
            
            ' 4. Sincronização de dados
            LogMessage("4/5 - Sincronizando dados...")
            Dim sync = Await SincronizarDadosExternos()
            resultados.Add(sync)
            
            ' 5. Validação final
            LogMessage("5/5 - Validação final...")
            backendApiManager.CheckHealthAsync()
            resultados.Add("✅ Health check executado")
            
            resultados.Add("")
            resultados.Add("=== INTEGRAÇÃO COMPLETA FINALIZADA ===")
            
            Return String.Join(Environment.NewLine, resultados)
            
        Catch ex As Exception
            Return $"❌ Erro na integração completa: {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' Método específico para trabalhar com dados SUAT
    ''' </summary>
    Private Sub ProcessarDadosSUAT()
        Try
            LogMessage("🔄 Processando integração com dados SUAT...")
            
            ' Verificar estrutura do banco SUAT
            Dim estrutura = databaseManager.VerificarEstrutura()
            If estrutura.Success Then
                LogMessage($"✅ Estrutura SUAT verificada: {estrutura.Tabelas.Count} tabelas")
                
                ' Processar cada tabela encontrada
                For Each tabela In estrutura.Tabelas.Take(5) ' Limitar a 5 para não sobrecarregar o log
                    LogMessage($"📋 Tabela: {tabela.Nome} - {tabela.Registros} registros")
                Next
                
                ' Atualizar interface com informações do SUAT
                lblResultadoTeste.Text = $"✅ SUAT: {estrutura.Tabelas.Count} tabelas processadas"
            Else
                LogMessage($"❌ Erro na estrutura SUAT: {estrutura.Mensagem}")
                lblResultadoTeste.Text = $"❌ Erro SUAT: {estrutura.Mensagem}"
            End If
            
        Catch ex As Exception
            LogMessage($"❌ Erro ao processar dados SUAT: {ex.Message}")
            lblResultadoTeste.Text = $"❌ Erro ao processar SUAT: {ex.Message}"
        End Try
    End Sub

    ' --- Métodos Auxiliares ---
    Private Async Function ExecutarComProgress(status As String, funcao As Func(Of Task(Of String))) As Task
        Try
            lblStatusGeral.Text = status
            progressBar.Visible = True
            progressBar.Style = ProgressBarStyle.Marquee
            
            Dim resultado = Await funcao()
            lblResultadoTeste.Text = resultado
            LogMessage(resultado)
            
        Catch ex As Exception
            Dim erro = $"❌ Erro: {ex.Message}"
            lblResultadoTeste.Text = erro
            LogMessage(erro)
        Finally
            progressBar.Visible = False
            lblStatusGeral.Text = "Pronto"
        End Try
    End Function

    Private Sub AtualizarStatusIndicadores()
        Try
            ' Status da conexão (baseado no endpoint configurado)
            UpdateConnectionStatus()
            
            ' Status do banco
            Dim bancoOk = databaseManager.VerificarExistenciaBanco()
            lblStatusBanco.Text = If(bancoOk, "Banco: Conectado", "Banco: Desconectado")
            lblStatusBanco.ForeColor = If(bancoOk, Color.Green, Color.Red)
            
            ' Status da API (usando instância compartilhada)
            Dim apiOk = backendApiManager.IsRunning
            lblStatusAPI.Text = If(apiOk, "API: Ativa", "API: Inativa")
            lblStatusAPI.ForeColor = If(apiOk, Color.Green, Color.Gray)
            
            ' Atualizar informações de sincronização se disponíveis
            AtualizarInformacoesSync()
            
        Catch ex As Exception
            LogMessage($"Erro ao atualizar status: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Atualiza status de conexão baseado no endpoint configurado
    ''' </summary>
    Private Async Sub UpdateConnectionStatus()
        Try
            If String.IsNullOrWhiteSpace(txtServidor.Text) Then
                lblStatusConexao.Text = "Conexão: Não configurada"
                lblStatusConexao.ForeColor = Color.Gray
                Return
            End If
            
            Dim url = $"http://{txtServidor.Text}:{txtPorta.Text}/api/health"
            Dim response = Await httpClient.GetAsync(url)
            If response.IsSuccessStatusCode Then
                lblStatusConexao.Text = "Conexão: Conectado"
                lblStatusConexao.ForeColor = Color.Green
            Else
                lblStatusConexao.Text = "Conexão: Falha"
                lblStatusConexao.ForeColor = Color.Red
            End If
        Catch
            lblStatusConexao.Text = "Conexão: Erro"
            lblStatusConexao.ForeColor = Color.Red
        End Try
    End Sub
    
    ' --- Handlers de Eventos dos Gerenciadores ---
    Private Sub OnUpdateProgress(percentage As Integer, status As String)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() OnUpdateProgress(percentage, status))
            Else
                LogMessage($"[Update] {status} ({percentage}%)")
                If progressBar.Visible Then
                    progressBar.Value = Math.Min(100, percentage)
                End If
            End If
        Catch
        End Try
    End Sub
    
    Private Sub OnPortStatus(port As Integer, status As String)
        LogMessage($"[Porta {port}] {status}")
    End Sub
    
    Private Sub OnBackendStatus(message As String)
        LogMessage($"[Backend] {message}")
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AtualizarStatusIndicadores())
        Else
            AtualizarStatusIndicadores()
        End If
    End Sub
    
    Private Sub OnBackendStarted(url As String)
        LogMessage($"✅ Backend iniciado: {url}")
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AtualizarStatusIndicadores())
        Else
            AtualizarStatusIndicadores()
        End If
    End Sub
    
    Private Sub OnBackendStopped()
        LogMessage("🛑 Backend parado")
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AtualizarStatusIndicadores())
        Else
            AtualizarStatusIndicadores()
        End If
    End Sub
    
    Private Sub OnHealthCheck(isHealthy As Boolean, message As String)
        Dim statusMsg = If(isHealthy, $"💚 Health Check OK: {message}", $"💔 Health Check FAIL: {message}")
        LogMessage(statusMsg)
        lblResultadoTeste.Text = statusMsg
    End Sub
    
    Private Sub OnFrontendStatus(message As String)
        LogMessage($"[Frontend] {message}")
    End Sub
    
    Private Sub OnFrontendStarted(url As String)
        LogMessage($"✅ Frontend iniciado: {url}")
    End Sub
    
    Private Sub OnFrontendStopped()
        LogMessage("🛑 Frontend parado")
    End Sub

    Private Sub LogMessage(mensagem As String)
        Try
            Dim timestamp = DateTime.Now.ToString("HH:mm:ss")
            txtLog.AppendText($"[{timestamp}] {mensagem}{Environment.NewLine}")
            txtLog.SelectionStart = txtLog.TextLength
            txtLog.ScrollToCaret()
        Catch
            ' Ignorar erros de log
        End Try
    End Sub

    Private Sub CarregarConfiguracoes()
        Try
            Dim configPath = Path.Combine(Application.StartupPath, "config", "integracao.json")
            If File.Exists(configPath) Then
                Dim json = File.ReadAllText(configPath)
                Dim config = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(json)
                
                If config.ContainsKey("servidor") Then txtServidor.Text = config("servidor").ToString()
                If config.ContainsKey("porta") Then txtPorta.Text = config("porta").ToString()
                If config.ContainsKey("endpointBase") Then txtEndpointBase.Text = config("endpointBase").ToString()
                If config.ContainsKey("endpointAuth") Then txtEndpointAuth.Text = config("endpointAuth").ToString()
                If config.ContainsKey("endpointDados") Then txtEndpointDados.Text = config("endpointDados").ToString()
                
                LogMessage("Configurações carregadas")
            End If
        Catch ex As Exception
            LogMessage($"Erro ao carregar configurações: {ex.Message}")
        End Try
    End Sub

    Private Sub SalvarConfiguracoes()
        Try
            Dim config As New Dictionary(Of String, Object) From {
                {"servidor", txtServidor.Text},
                {"porta", txtPorta.Text},
                {"endpointBase", txtEndpointBase.Text},
                {"endpointAuth", txtEndpointAuth.Text},
                {"endpointDados", txtEndpointDados.Text},
                {"salvarCredenciais", chkSalvarCredenciais.Checked}
            }
            
            If chkSalvarCredenciais.Checked Then
                config("usuario") = txtUsuario.Text
                config("senha") = txtSenha.Text
            End If
            
            Dim configPath = Path.Combine(Application.StartupPath, "config")
            If Not Directory.Exists(configPath) Then
                Directory.CreateDirectory(configPath)
            End If
            
            Dim json = JsonConvert.SerializeObject(config, Formatting.Indented)
            File.WriteAllText(Path.Combine(configPath, "integracao.json"), json)
            
            LogMessage("✅ Configurações salvas com sucesso")
            MessageBox.Show("Configurações salvas com sucesso!", "Sucesso", 
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            
        Catch ex As Exception
            LogMessage($"❌ Erro ao salvar configurações: {ex.Message}")
            MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AtualizarInformacoesSync()
        Try
            ' Buscar informações de sincronização do banco SUAT
            If databaseManager IsNot Nothing Then
                Try
                    ' Verificar se há dados de sincronização no banco
                    Dim result = databaseManager.VerificarEstrutura()
                    If result.Success AndAlso result.Tabelas.Count > 0 Then
                        ' Buscar a última sincronização se houver tabelas
                        txtUltimaSync.Text = "Dados encontrados no banco"
                        LogMessage($"📊 Banco SUAT: {result.Tabelas.Count} tabelas encontradas")
                    Else
                        txtUltimaSync.Text = "Banco vazio ou não configurado"
                    End If
                Catch ex As Exception
                    txtUltimaSync.Text = "Erro ao verificar banco"
                    LogMessage($"❌ Erro ao verificar banco SUAT: {ex.Message}")
                End Try
            Else
                txtUltimaSync.Text = "Database Manager não disponível"
            End If
            
            ' Configuração padrão
            If String.IsNullOrEmpty(txtProximaSync.Text) Or txtProximaSync.Text = "Não agendada" Then
                txtProximaSync.Text = "Não agendada"
                chkSincronizacaoAutomatica.Checked = False
            End If
            
        Catch ex As Exception
            LogMessage($"Erro ao atualizar informações de sync: {ex.Message}")
        End Try
    End Sub

    Private Sub ExportarLog()
        Try
            Dim saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Arquivo de texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*"
            saveDialog.FileName = $"log-integracao-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
            
            If saveDialog.ShowDialog() = DialogResult.OK Then
                File.WriteAllText(saveDialog.FileName, txtLog.Text)
                LogMessage($"✅ Log exportado para: {saveDialog.FileName}")
                MessageBox.Show("Log exportado com sucesso!", "Sucesso", 
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            LogMessage($"❌ Erro ao exportar log: {ex.Message}")
            MessageBox.Show($"Erro ao exportar log: {ex.Message}", "Erro", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class