Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Text.RegularExpressions
Imports System.ComponentModel

''' <summarybtnLimpar
''' Sistema Profissional de Gestão de Logs
''' Interface moderna com funcionalidades avançadas de filtragem, busca e análise
''' </summary>
Public Class LogViewerFormProfessional
    Inherits Form

    ' --- Controles da UI Principal ---
    Private WithEvents rtbLogs As RichTextBox
    Private statusStrip As StatusStrip
    Private mainToolStrip As ToolStrip
    Private panelFilters As Panel
    Private panelSearch As Panel
    Private panelStats As Panel
    Private panelHeader As Panel
    Private splitterMain As Splitter
    Private panelMiniMap As Panel

    ' Cards
    Private panelCardSearch As Panel
    Private panelCardFilters As Panel
    Private panelCardStats As Panel
    Private lblBadgeResults As Label
    Private lblBadgeFilters As Label
    Private lblBadgeTotal As Label

    ' --- ToolStrip Buttons ---
    Private WithEvents btnLimpar As ToolStripButton
    Private WithEvents btnExportar As ToolStripButton
    Private WithEvents btnConfiguracoes As ToolStripButton
    Private WithEvents btnEstatisticas As ToolStripButton
    Private WithEvents btnPausar As ToolStripButton
    Private WithEvents btnFechar As ToolStripButton
    Private WithEvents btnDrawer As ToolStripButton

    ' --- Filtros ---
    Private WithEvents chkAutoScroll As CheckBox
    Private WithEvents chkTimestamp As CheckBox
    Private WithEvents chkInfo As CheckBox
    Private WithEvents chkWarning As CheckBox
    Private WithEvents chkError As CheckBox
    Private WithEvents chkSuccess As CheckBox
    Private WithEvents chkDebug As CheckBox
    Private WithEvents cmbNivelMinimo As ComboBox

    ' --- Busca ---
    Private WithEvents txtBusca As TextBox
    Private WithEvents btnBuscar As Button
    Private WithEvents btnProximo As Button
    Private WithEvents btnAnterior As Button
    Private lblResultados As Label

    ' --- Estatísticas ---
    Private lblTotalInfo As Label
    Private lblTotalWarning As Label
    Private lblTotalError As Label
    Private lblTotalSuccess As Label
    Private lblTotalDebug As Label
    Private progressMemoria As ProgressBar

    ' --- Status Bar ---
    Private lblTotalLinhas As ToolStripStatusLabel
    Private lblUltimaAtualizacao As ToolStripStatusLabel
    Private lblStatus As ToolStripStatusLabel
    Private lblFiltrados As ToolStripStatusLabel

    ' --- Context Menu ---
    Private ctxLogMenu As ContextMenuStrip
    Private WithEvents menuCopy As ToolStripMenuItem
    Private WithEvents menuCopyAll As ToolStripMenuItem
    Private WithEvents menuSaveSelection As ToolStripMenuItem
    Private WithEvents menuSaveAll As ToolStripMenuItem
    Private WithEvents menuClear As ToolStripMenuItem
    Private WithEvents menuToggleWrap As ToolStripMenuItem
    Private WithEvents menuIncreaseFont As ToolStripMenuItem
    Private WithEvents menuDecreaseFont As ToolStripMenuItem

    ' --- Gerenciamento de Logs ---
    Private Shared _logBuffer As New List(Of LogEntry)
    Private Shared _maxBufferSize As Integer = 10000
    Private Shared _instance As LogViewerFormProfessional
    Private lastDisplayedCount As Integer = 0
    Private filteredLogs As List(Of LogEntry)
    Private currentSearchPositions As List(Of Integer)
    Private currentSearchIndex As Integer = -1
    Private isPaused As Boolean = False
    Private showStatsPanel As Boolean = True
    Private lastSearchText As String = ""
    Private useVividColors As Boolean = True
    Private currentSelectionStart As Integer = -1
    Private previousActiveIndex As Integer = -1
    Private ReadOnly defaultHighlightColor As Color = Color.FromArgb(75, 110, 175) ' azul
    Private ReadOnly activeHighlightColor As Color = Color.FromArgb(255, 170, 0)   ' âmbar
    Friend WithEvents dtTitle As Label
    Friend WithEvents dtDrawerTitle As Label
    Friend WithEvents dtCardSearch As Panel
    Friend WithEvents dtLblBusca As Label
    Friend WithEvents dtTxtBusca As TextBox
    Friend WithEvents dtBtnPrev As Button
    Friend WithEvents dtBtnNext As Button
    Friend WithEvents dtCardFilters As Panel
    Friend WithEvents dtLblFiltros As Label
    Friend WithEvents dtChkInfo As CheckBox
    Friend WithEvents dtChkWarn As CheckBox
    Friend WithEvents dtChkErr As CheckBox
    Friend WithEvents dtChkOk As CheckBox
    Friend WithEvents dtChkDbg As CheckBox
    Friend WithEvents dtCardStats As Panel
    Friend WithEvents dtLblStats As Label
    Friend WithEvents dtLblInfo As Label
    Friend WithEvents dtLblWarn As Label
    Friend WithEvents dtLblErr As Label
    Friend WithEvents dtLblOk As Label
    Friend WithEvents dtProg As ProgressBar
    Private ReadOnly lineHighlightColor As Color = Color.FromArgb(55, 55, 55)      ' linha ativa

    ''' <summary>
    ''' Estrutura para entrada de log
    ''' </summary>
    Public Structure LogEntry
        Public Timestamp As DateTime
        Public Message As String
        Public LogType As LogType

        Public Sub New(message As String, Optional logType As LogType = LogType.Info)
            Me.Timestamp = DateTime.Now
            Me.Message = message
            Me.LogType = logType
        End Sub
    End Structure

    ''' <summary>
    ''' Tipos de log para colorização
    ''' </summary>
    Public Enum LogType
        Info
        Warning
        [Error]
        Success
        Debug
    End Enum

    ''' <summary>
    ''' Propriedade para obter a instância atual (Singleton pattern)
    ''' </summary>
    Public Shared ReadOnly Property Instance As LogViewerFormProfessional
        Get
            If _instance Is Nothing OrElse _instance.IsDisposed Then
                _instance = New LogViewerFormProfessional()
            End If
            Return _instance
        End Get
    End Property

    ''' <summary>
    ''' Construtor do formulário
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        InitializeData()
        ' Em runtime, construir a UI completa fora do InitializeComponent
        If LicenseManager.UsageMode <> LicenseUsageMode.Designtime Then
            Try
                Me.SuspendLayout()
                Me.Controls.Clear()
                BuildControls()
                BuildToolStrip()
                BuildDrawerPanels()
                BuildStatusStrip()
                ApplyLayout()
                ApplyFormProperties()
            Finally
                Me.ResumeLayout(False)
                Me.PerformLayout()
            End Try

            LoadBufferedLogs()
            UpdateStatus()
            UpdateStatistics()
        End If
    End Sub

    ''' <summary>
    ''' Inicializa o menu de contexto do log
    ''' </summary>
    Private Sub InitializeContextMenu()
        Me.ctxLogMenu = New ContextMenuStrip()
        Me.ctxLogMenu.BackColor = Color.FromArgb(45, 45, 48)
        Me.ctxLogMenu.ForeColor = Color.White

        Me.menuCopy = New ToolStripMenuItem("Copiar Seleção")
        Me.menuCopyAll = New ToolStripMenuItem("Copiar Tudo")
        Me.menuSaveSelection = New ToolStripMenuItem("Salvar Seleção...")
        Me.menuSaveAll = New ToolStripMenuItem("Salvar Tudo...")
        Me.menuClear = New ToolStripMenuItem("Limpar")
        Me.menuToggleWrap = New ToolStripMenuItem("Alternar Quebra de Linha")
        Me.menuIncreaseFont = New ToolStripMenuItem("Aumentar Fonte")
        Me.menuDecreaseFont = New ToolStripMenuItem("Diminuir Fonte")

        Me.ctxLogMenu.Items.AddRange(New ToolStripItem() {
            Me.menuCopy, Me.menuCopyAll, New ToolStripSeparator(),
            Me.menuSaveSelection, Me.menuSaveAll, New ToolStripSeparator(),
            Me.menuToggleWrap, New ToolStripSeparator(),
            Me.menuIncreaseFont, Me.menuDecreaseFont, New ToolStripSeparator(),
            Me.menuClear
        })
    End Sub

    ''' <summary>
    ''' Inicializa estruturas de dados
    ''' </summary>
    Private Sub InitializeData()
        filteredLogs = New List(Of LogEntry)
        currentSearchPositions = New List(Of Integer)
    End Sub

    ''' <summary>
    ''' Inicializa os componentes da interface
    ''' </summary>
    Private Sub InitializeComponent()
        Me.mainToolStrip = New System.Windows.Forms.ToolStrip()
        Me.btnLimpar = New System.Windows.Forms.ToolStripButton()
        Me.btnExportar = New System.Windows.Forms.ToolStripButton()
        Me.btnPausar = New System.Windows.Forms.ToolStripButton()
        Me.btnEstatisticas = New System.Windows.Forms.ToolStripButton()
        Me.btnDrawer = New System.Windows.Forms.ToolStripButton()
        Me.btnConfiguracoes = New System.Windows.Forms.ToolStripButton()
        Me.btnFechar = New System.Windows.Forms.ToolStripButton()
        Me.panelHeader = New System.Windows.Forms.Panel()
        Me.dtTitle = New System.Windows.Forms.Label()
        Me.panelStats = New System.Windows.Forms.Panel()
        Me.dtDrawerTitle = New System.Windows.Forms.Label()
        Me.dtCardSearch = New System.Windows.Forms.Panel()
        Me.dtLblBusca = New System.Windows.Forms.Label()
        Me.dtTxtBusca = New System.Windows.Forms.TextBox()
        Me.dtBtnPrev = New System.Windows.Forms.Button()
        Me.dtBtnNext = New System.Windows.Forms.Button()
        Me.dtCardFilters = New System.Windows.Forms.Panel()
        Me.dtLblFiltros = New System.Windows.Forms.Label()
        Me.dtChkInfo = New System.Windows.Forms.CheckBox()
        Me.dtChkWarn = New System.Windows.Forms.CheckBox()
        Me.dtChkErr = New System.Windows.Forms.CheckBox()
        Me.dtChkOk = New System.Windows.Forms.CheckBox()
        Me.dtChkDbg = New System.Windows.Forms.CheckBox()
        Me.dtCardStats = New System.Windows.Forms.Panel()
        Me.dtLblStats = New System.Windows.Forms.Label()
        Me.dtLblInfo = New System.Windows.Forms.Label()
        Me.dtLblWarn = New System.Windows.Forms.Label()
        Me.dtLblErr = New System.Windows.Forms.Label()
        Me.dtLblOk = New System.Windows.Forms.Label()
        Me.dtProg = New System.Windows.Forms.ProgressBar()
        Me.rtbLogs = New System.Windows.Forms.RichTextBox()
        Me.statusStrip = New System.Windows.Forms.StatusStrip()
        Me.mainToolStrip.SuspendLayout()
        Me.panelHeader.SuspendLayout()
        Me.panelStats.SuspendLayout()
        Me.dtCardSearch.SuspendLayout()
        Me.dtCardFilters.SuspendLayout()
        Me.dtCardStats.SuspendLayout()
        Me.SuspendLayout()
        '
        'mainToolStrip
        '
        Me.mainToolStrip.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
        Me.mainToolStrip.ForeColor = System.Drawing.Color.White
        Me.mainToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden
        Me.mainToolStrip.ImageScalingSize = New System.Drawing.Size(28, 28)
        Me.mainToolStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnLimpar, Me.btnExportar, Me.btnPausar, Me.btnEstatisticas, Me.btnDrawer, Me.btnConfiguracoes, Me.btnFechar})
        Me.mainToolStrip.Location = New System.Drawing.Point(0, 0)
        Me.mainToolStrip.Name = "mainToolStrip"
        Me.mainToolStrip.Size = New System.Drawing.Size(1531, 44)
        Me.mainToolStrip.TabIndex = 3
        '
        'btnLimpar
        '
        Me.btnLimpar.ForeColor = System.Drawing.Color.White
        Me.btnLimpar.Name = "btnLimpar"
        Me.btnLimpar.Size = New System.Drawing.Size(115, 38)
        Me.btnLimpar.Text = "🗑️ Limpar"
        '
        'btnExportar
        '
        Me.btnExportar.ForeColor = System.Drawing.Color.White
        Me.btnExportar.Name = "btnExportar"
        Me.btnExportar.Size = New System.Drawing.Size(127, 38)
        Me.btnExportar.Text = "💾 Exportar"
        '
        'btnPausar
        '
        Me.btnPausar.ForeColor = System.Drawing.Color.White
        Me.btnPausar.Name = "btnPausar"
        Me.btnPausar.Size = New System.Drawing.Size(113, 38)
        Me.btnPausar.Text = "⏸️ Pausar"
        '
        'btnEstatisticas
        '
        Me.btnEstatisticas.ForeColor = System.Drawing.Color.White
        Me.btnEstatisticas.Name = "btnEstatisticas"
        Me.btnEstatisticas.Size = New System.Drawing.Size(94, 38)
        Me.btnEstatisticas.Text = "📊 Stats"
        '
        'btnDrawer
        '
        Me.btnDrawer.ForeColor = System.Drawing.Color.White
        Me.btnDrawer.Name = "btnDrawer"
        Me.btnDrawer.Size = New System.Drawing.Size(118, 38)
        Me.btnDrawer.Text = "📥 Drawer"
        '
        'btnConfiguracoes
        '
        Me.btnConfiguracoes.ForeColor = System.Drawing.Color.White
        Me.btnConfiguracoes.Name = "btnConfiguracoes"
        Me.btnConfiguracoes.Size = New System.Drawing.Size(113, 38)
        Me.btnConfiguracoes.Text = "⚙️ Config"
        '
        'btnFechar
        '
        Me.btnFechar.ForeColor = System.Drawing.Color.White
        Me.btnFechar.Name = "btnFechar"
        Me.btnFechar.Size = New System.Drawing.Size(113, 38)
        Me.btnFechar.Text = "❌ Fechar"
        '
        'panelHeader
        '
        Me.panelHeader.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(35, Byte), Integer), CType(CType(38, Byte), Integer))
        Me.panelHeader.Controls.Add(Me.dtTitle)
        Me.panelHeader.Dock = System.Windows.Forms.DockStyle.Top
        Me.panelHeader.Location = New System.Drawing.Point(0, 44)
        Me.panelHeader.Name = "panelHeader"
        Me.panelHeader.Size = New System.Drawing.Size(1531, 104)
        Me.panelHeader.TabIndex = 2
        '
        'dtTitle
        '
        Me.dtTitle.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.dtTitle.ForeColor = System.Drawing.Color.White
        Me.dtTitle.Location = New System.Drawing.Point(10, 12)
        Me.dtTitle.Name = "dtTitle"
        Me.dtTitle.Size = New System.Drawing.Size(400, 61)
        Me.dtTitle.TabIndex = 0
        Me.dtTitle.Text = "📋 Visualizador de Logs"
        '
        'panelStats
        '
        Me.panelStats.BackColor = System.Drawing.Color.FromArgb(CType(CType(37, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(38, Byte), Integer))
        Me.panelStats.Controls.Add(Me.dtDrawerTitle)
        Me.panelStats.Controls.Add(Me.dtCardSearch)
        Me.panelStats.Controls.Add(Me.dtCardFilters)
        Me.panelStats.Controls.Add(Me.dtCardStats)
        Me.panelStats.Dock = System.Windows.Forms.DockStyle.Right
        Me.panelStats.Location = New System.Drawing.Point(1251, 148)
        Me.panelStats.Name = "panelStats"
        Me.panelStats.Size = New System.Drawing.Size(280, 820)
        Me.panelStats.TabIndex = 1
        '
        'dtDrawerTitle
        '
        Me.dtDrawerTitle.ForeColor = System.Drawing.Color.Gainsboro
        Me.dtDrawerTitle.Location = New System.Drawing.Point(10, 10)
        Me.dtDrawerTitle.Name = "dtDrawerTitle"
        Me.dtDrawerTitle.Size = New System.Drawing.Size(250, 20)
        Me.dtDrawerTitle.TabIndex = 0
        Me.dtDrawerTitle.Text = "Drawer (Busca / Filtros / Estatísticas)"
        '
        'dtCardSearch
        '
        Me.dtCardSearch.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
        Me.dtCardSearch.Controls.Add(Me.dtLblBusca)
        Me.dtCardSearch.Controls.Add(Me.dtTxtBusca)
        Me.dtCardSearch.Controls.Add(Me.dtBtnPrev)
        Me.dtCardSearch.Controls.Add(Me.dtBtnNext)
        Me.dtCardSearch.Dock = System.Windows.Forms.DockStyle.Top
        Me.dtCardSearch.Location = New System.Drawing.Point(0, 150)
        Me.dtCardSearch.Name = "dtCardSearch"
        Me.dtCardSearch.Size = New System.Drawing.Size(280, 110)
        Me.dtCardSearch.TabIndex = 1
        '
        'dtLblBusca
        '
        Me.dtLblBusca.ForeColor = System.Drawing.Color.White
        Me.dtLblBusca.Location = New System.Drawing.Point(10, 10)
        Me.dtLblBusca.Name = "dtLblBusca"
        Me.dtLblBusca.Size = New System.Drawing.Size(200, 20)
        Me.dtLblBusca.TabIndex = 0
        Me.dtLblBusca.Text = "🔎 Busca"
        '
        'dtTxtBusca
        '
        Me.dtTxtBusca.Location = New System.Drawing.Point(10, 40)
        Me.dtTxtBusca.Name = "dtTxtBusca"
        Me.dtTxtBusca.Size = New System.Drawing.Size(180, 29)
        Me.dtTxtBusca.TabIndex = 1
        '
        'dtBtnPrev
        '
        Me.dtBtnPrev.Location = New System.Drawing.Point(200, 38)
        Me.dtBtnPrev.Name = "dtBtnPrev"
        Me.dtBtnPrev.Size = New System.Drawing.Size(70, 26)
        Me.dtBtnPrev.TabIndex = 2
        Me.dtBtnPrev.Text = "↑ Anterior"
        '
        'dtBtnNext
        '
        Me.dtBtnNext.Location = New System.Drawing.Point(200, 68)
        Me.dtBtnNext.Name = "dtBtnNext"
        Me.dtBtnNext.Size = New System.Drawing.Size(70, 26)
        Me.dtBtnNext.TabIndex = 3
        Me.dtBtnNext.Text = "↓ Próximo"
        '
        'dtCardFilters
        '
        Me.dtCardFilters.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
        Me.dtCardFilters.Controls.Add(Me.dtLblFiltros)
        Me.dtCardFilters.Controls.Add(Me.dtChkInfo)
        Me.dtCardFilters.Controls.Add(Me.dtChkWarn)
        Me.dtCardFilters.Controls.Add(Me.dtChkErr)
        Me.dtCardFilters.Controls.Add(Me.dtChkOk)
        Me.dtCardFilters.Controls.Add(Me.dtChkDbg)
        Me.dtCardFilters.Dock = System.Windows.Forms.DockStyle.Top
        Me.dtCardFilters.Location = New System.Drawing.Point(0, 0)
        Me.dtCardFilters.Name = "dtCardFilters"
        Me.dtCardFilters.Size = New System.Drawing.Size(280, 150)
        Me.dtCardFilters.TabIndex = 2
        '
        'dtLblFiltros
        '
        Me.dtLblFiltros.ForeColor = System.Drawing.Color.White
        Me.dtLblFiltros.Location = New System.Drawing.Point(10, 10)
        Me.dtLblFiltros.Name = "dtLblFiltros"
        Me.dtLblFiltros.Size = New System.Drawing.Size(200, 20)
        Me.dtLblFiltros.TabIndex = 0
        Me.dtLblFiltros.Text = "🧰 Filtros"
        '
        'dtChkInfo
        '
        Me.dtChkInfo.Checked = True
        Me.dtChkInfo.CheckState = System.Windows.Forms.CheckState.Checked
        Me.dtChkInfo.ForeColor = System.Drawing.Color.Gainsboro
        Me.dtChkInfo.Location = New System.Drawing.Point(10, 40)
        Me.dtChkInfo.Name = "dtChkInfo"
        Me.dtChkInfo.Size = New System.Drawing.Size(104, 24)
        Me.dtChkInfo.TabIndex = 1
        Me.dtChkInfo.Text = "ℹ️ Info"
        '
        'dtChkWarn
        '
        Me.dtChkWarn.Checked = True
        Me.dtChkWarn.CheckState = System.Windows.Forms.CheckState.Checked
        Me.dtChkWarn.ForeColor = System.Drawing.Color.Gold
        Me.dtChkWarn.Location = New System.Drawing.Point(90, 40)
        Me.dtChkWarn.Name = "dtChkWarn"
        Me.dtChkWarn.Size = New System.Drawing.Size(104, 24)
        Me.dtChkWarn.TabIndex = 2
        Me.dtChkWarn.Text = "⚠️ Warning"
        '
        'dtChkErr
        '
        Me.dtChkErr.Checked = True
        Me.dtChkErr.CheckState = System.Windows.Forms.CheckState.Checked
        Me.dtChkErr.ForeColor = System.Drawing.Color.Tomato
        Me.dtChkErr.Location = New System.Drawing.Point(200, 40)
        Me.dtChkErr.Name = "dtChkErr"
        Me.dtChkErr.Size = New System.Drawing.Size(104, 24)
        Me.dtChkErr.TabIndex = 3
        Me.dtChkErr.Text = "❌ Error"
        '
        'dtChkOk
        '
        Me.dtChkOk.Checked = True
        Me.dtChkOk.CheckState = System.Windows.Forms.CheckState.Checked
        Me.dtChkOk.ForeColor = System.Drawing.Color.Lime
        Me.dtChkOk.Location = New System.Drawing.Point(10, 70)
        Me.dtChkOk.Name = "dtChkOk"
        Me.dtChkOk.Size = New System.Drawing.Size(104, 24)
        Me.dtChkOk.TabIndex = 4
        Me.dtChkOk.Text = "✅ Success"
        '
        'dtChkDbg
        '
        Me.dtChkDbg.Checked = True
        Me.dtChkDbg.CheckState = System.Windows.Forms.CheckState.Checked
        Me.dtChkDbg.ForeColor = System.Drawing.Color.DeepSkyBlue
        Me.dtChkDbg.Location = New System.Drawing.Point(120, 70)
        Me.dtChkDbg.Name = "dtChkDbg"
        Me.dtChkDbg.Size = New System.Drawing.Size(104, 24)
        Me.dtChkDbg.TabIndex = 5
        Me.dtChkDbg.Text = "🔧 Debug"
        '
        'dtCardStats
        '
        Me.dtCardStats.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
        Me.dtCardStats.Controls.Add(Me.dtLblStats)
        Me.dtCardStats.Controls.Add(Me.dtLblInfo)
        Me.dtCardStats.Controls.Add(Me.dtLblWarn)
        Me.dtCardStats.Controls.Add(Me.dtLblErr)
        Me.dtCardStats.Controls.Add(Me.dtLblOk)
        Me.dtCardStats.Controls.Add(Me.dtProg)
        Me.dtCardStats.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.dtCardStats.Location = New System.Drawing.Point(0, 640)
        Me.dtCardStats.Name = "dtCardStats"
        Me.dtCardStats.Size = New System.Drawing.Size(280, 180)
        Me.dtCardStats.TabIndex = 3
        '
        'dtLblStats
        '
        Me.dtLblStats.ForeColor = System.Drawing.Color.White
        Me.dtLblStats.Location = New System.Drawing.Point(10, 10)
        Me.dtLblStats.Name = "dtLblStats"
        Me.dtLblStats.Size = New System.Drawing.Size(200, 20)
        Me.dtLblStats.TabIndex = 0
        Me.dtLblStats.Text = "📊 Estatísticas"
        '
        'dtLblInfo
        '
        Me.dtLblInfo.ForeColor = System.Drawing.Color.Gainsboro
        Me.dtLblInfo.Location = New System.Drawing.Point(10, 40)
        Me.dtLblInfo.Name = "dtLblInfo"
        Me.dtLblInfo.Size = New System.Drawing.Size(100, 23)
        Me.dtLblInfo.TabIndex = 1
        Me.dtLblInfo.Text = "ℹ️ Info: 0"
        '
        'dtLblWarn
        '
        Me.dtLblWarn.ForeColor = System.Drawing.Color.Gold
        Me.dtLblWarn.Location = New System.Drawing.Point(10, 65)
        Me.dtLblWarn.Name = "dtLblWarn"
        Me.dtLblWarn.Size = New System.Drawing.Size(100, 23)
        Me.dtLblWarn.TabIndex = 2
        Me.dtLblWarn.Text = "⚠️ Warning: 0"
        '
        'dtLblErr
        '
        Me.dtLblErr.ForeColor = System.Drawing.Color.Tomato
        Me.dtLblErr.Location = New System.Drawing.Point(10, 90)
        Me.dtLblErr.Name = "dtLblErr"
        Me.dtLblErr.Size = New System.Drawing.Size(100, 23)
        Me.dtLblErr.TabIndex = 3
        Me.dtLblErr.Text = "❌ Error: 0"
        '
        'dtLblOk
        '
        Me.dtLblOk.ForeColor = System.Drawing.Color.Lime
        Me.dtLblOk.Location = New System.Drawing.Point(10, 115)
        Me.dtLblOk.Name = "dtLblOk"
        Me.dtLblOk.Size = New System.Drawing.Size(100, 23)
        Me.dtLblOk.TabIndex = 4
        Me.dtLblOk.Text = "✅ Success: 0"
        '
        'dtProg
        '
        Me.dtProg.Location = New System.Drawing.Point(10, 140)
        Me.dtProg.Name = "dtProg"
        Me.dtProg.Size = New System.Drawing.Size(250, 18)
        Me.dtProg.TabIndex = 5
        '
        'rtbLogs
        '
        Me.rtbLogs.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.rtbLogs.Dock = System.Windows.Forms.DockStyle.Fill
        Me.rtbLogs.ForeColor = System.Drawing.Color.LightGray
        Me.rtbLogs.Location = New System.Drawing.Point(0, 148)
        Me.rtbLogs.Name = "rtbLogs"
        Me.rtbLogs.ReadOnly = True
        Me.rtbLogs.Size = New System.Drawing.Size(1251, 820)
        Me.rtbLogs.TabIndex = 0
        Me.rtbLogs.Text = ""
        '
        'statusStrip
        '
        Me.statusStrip.ImageScalingSize = New System.Drawing.Size(28, 28)
        Me.statusStrip.Location = New System.Drawing.Point(0, 968)
        Me.statusStrip.Name = "statusStrip"
        Me.statusStrip.Size = New System.Drawing.Size(1531, 22)
        Me.statusStrip.SizingGrip = False
        Me.statusStrip.TabIndex = 4
        '
        'LogViewerFormProfessional
        '
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1531, 990)
        Me.Controls.Add(Me.rtbLogs)
        Me.Controls.Add(Me.panelStats)
        Me.Controls.Add(Me.panelHeader)
        Me.Controls.Add(Me.mainToolStrip)
        Me.Controls.Add(Me.statusStrip)
        Me.MinimumSize = New System.Drawing.Size(900, 600)
        Me.Name = "LogViewerFormProfessional"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "🎯 SUAT-IA - Sistema Profissional de Gestão de Logs"
        Me.mainToolStrip.ResumeLayout(False)
        Me.mainToolStrip.PerformLayout()
        Me.panelHeader.ResumeLayout(False)
        Me.panelStats.ResumeLayout(False)
        Me.dtCardSearch.ResumeLayout(False)
        Me.dtCardSearch.PerformLayout()
        Me.dtCardFilters.ResumeLayout(False)
        Me.dtCardStats.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    ''' <summary>
    ''' Inicializa controles básicos
    ''' </summary>
    Private Sub BuildControls()
        ' RichTextBox principal
        Me.rtbLogs = New RichTextBox()
        Me.rtbLogs.BackColor = Color.FromArgb(30, 30, 30)
        Me.rtbLogs.ForeColor = Color.LightGray
        Me.rtbLogs.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.rtbLogs.ReadOnly = True
        Me.rtbLogs.WordWrap = False
        Me.rtbLogs.ScrollBars = RichTextBoxScrollBars.Both
        Me.rtbLogs.BorderStyle = BorderStyle.None
        Me.rtbLogs.Dock = DockStyle.Fill
        ' Context menu
        InitializeContextMenu()
        Me.rtbLogs.ContextMenuStrip = Me.ctxLogMenu

        ' Cabeçalho
        Me.panelHeader = New Panel()
        Me.panelHeader.Dock = DockStyle.Top
        Me.panelHeader.Height = 56
        Me.panelHeader.BackColor = Color.FromArgb(35, 35, 38)
        Dim lblTitle As New Label()
        lblTitle.Text = "📋 Visualizador de Logs"
        lblTitle.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.Location = New Point(12, 8)
        lblTitle.Size = New Size(600, 24)
        Dim lblSubtitle As New Label()
        lblSubtitle.Text = "Monitoramento e análise em tempo real"
        lblSubtitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
        lblSubtitle.ForeColor = Color.FromArgb(180, 180, 180)
        lblSubtitle.Location = New Point(14, 32)
        lblSubtitle.Size = New Size(600, 18)
        Me.panelHeader.Controls.Add(lblTitle)
        Me.panelHeader.Controls.Add(lblSubtitle)

        ' Splitter
        Me.splitterMain = New Splitter()
        Me.splitterMain.Dock = DockStyle.Right
        Me.splitterMain.Width = 3
        Me.splitterMain.BackColor = Color.FromArgb(70, 70, 70)
    End Sub

    ''' <summary>
    ''' Inicializa a ToolStrip
    ''' </summary>
    Private Sub BuildToolStrip()
        Me.mainToolStrip = New ToolStrip()
        Me.mainToolStrip.BackColor = Color.FromArgb(45, 45, 48)
        Me.mainToolStrip.ForeColor = Color.White
        Me.mainToolStrip.GripStyle = ToolStripGripStyle.Hidden
        Me.mainToolStrip.RenderMode = ToolStripRenderMode.Professional
        Me.mainToolStrip.ImageScalingSize = New Size(20, 20)
        ' Renderer dark personalizado (bordas e separadores discretos)
        Me.mainToolStrip.Renderer = New ToolStripProfessionalRenderer(New DarkColorTable())

        ' Botões da toolbar
        Me.btnLimpar = New ToolStripButton("🗑️ Limpar")
        Me.btnLimpar.ToolTipText = "Limpar todos os logs"
        Me.btnLimpar.ForeColor = Color.White

        Me.btnExportar = New ToolStripButton("💾 Exportar")
        Me.btnExportar.ToolTipText = "Exportar logs para arquivo"
        Me.btnExportar.ForeColor = Color.White

        Me.btnPausar = New ToolStripButton("⏸️ Pausar")
        Me.btnPausar.ToolTipText = "Pausar/Retomar captura de logs"
        Me.btnPausar.ForeColor = Color.White

        Me.btnEstatisticas = New ToolStripButton("📊 Stats")
        Me.btnEstatisticas.ToolTipText = "Mostrar/Ocultar painel de estatísticas"
        Me.btnEstatisticas.ForeColor = Color.White

        Me.btnConfiguracoes = New ToolStripButton("⚙️ Config")
        Me.btnConfiguracoes.ToolTipText = "Preferências de exibição"
        Me.btnConfiguracoes.ForeColor = Color.White

        Me.btnDrawer = New ToolStripButton("📥 Drawer")
        Me.btnDrawer.ToolTipText = "Colapsar/Expandir lateral"
        Me.btnDrawer.ForeColor = Color.White

        Me.btnFechar = New ToolStripButton("❌ Fechar")
        Me.btnFechar.ToolTipText = "Fechar visualizador"
        Me.btnFechar.ForeColor = Color.White

        ' Adicionar à toolbar
        Me.mainToolStrip.Items.Add(btnLimpar)
        Me.mainToolStrip.Items.Add(New ToolStripSeparator())
        Me.mainToolStrip.Items.Add(btnExportar)
        Me.mainToolStrip.Items.Add(New ToolStripSeparator())
        Me.mainToolStrip.Items.Add(btnPausar)
        Me.mainToolStrip.Items.Add(btnEstatisticas)
        Me.mainToolStrip.Items.Add(New ToolStripSeparator())
        Me.mainToolStrip.Items.Add(btnDrawer)
        Me.mainToolStrip.Items.Add(New ToolStripSeparator())
        Me.mainToolStrip.Items.Add(btnConfiguracoes)
        Me.mainToolStrip.Items.Add(New ToolStripSeparator())
        Me.mainToolStrip.Items.Add(btnFechar)
    End Sub

    ' --- Renderer Dark Customizado ---
    Private Class DarkColorTable
        Inherits ProfessionalColorTable
        Public Overrides ReadOnly Property ToolStripGradientBegin As Color
            Get
                Return Color.FromArgb(45, 45, 48)
            End Get
        End Property
        Public Overrides ReadOnly Property ToolStripGradientMiddle As Color
            Get
                Return Color.FromArgb(45, 45, 48)
            End Get
        End Property
        Public Overrides ReadOnly Property ToolStripGradientEnd As Color
            Get
                Return Color.FromArgb(45, 45, 48)
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorDark As Color
            Get
                Return Color.FromArgb(70, 70, 70)
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorLight As Color
            Get
                Return Color.FromArgb(70, 70, 70)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelected As Color
            Get
                Return Color.FromArgb(63, 63, 70)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemBorder As Color
            Get
                Return Color.FromArgb(90, 90, 90)
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Inicializa os painéis laterais
    ''' </summary>
    Private Sub BuildDrawerPanels()
        ' Painel principal de estatísticas/filtros
        Me.panelStats = New Panel()
        Me.panelStats.BackColor = Color.FromArgb(37, 37, 38)
        Me.panelStats.Dock = DockStyle.Right
        Me.panelStats.Width = 300
        Me.panelStats.Padding = New Padding(10)

        ' Painel de filtros
        ' Card: Busca
        Me.panelCardSearch = CreateCardPanel("🔎 BUSCA", 0)
        Me.panelSearch = New Panel()
        Me.panelSearch.BackColor = Color.FromArgb(45, 45, 48)
        Me.panelSearch.Height = 130
        Me.panelSearch.Dock = DockStyle.Top
        Me.panelCardSearch.Controls.Add(Me.panelSearch)

        ' Card: Filtros
        Me.panelCardFilters = CreateCardPanel("🧰 FILTROS", 1)
        Me.panelFilters = New Panel()
        Me.panelFilters.BackColor = Color.FromArgb(45, 45, 48)
        Me.panelFilters.Height = 210
        Me.panelFilters.Dock = DockStyle.Top
        Me.panelCardFilters.Controls.Add(Me.panelFilters)

        ' Card: Estatísticas
        Me.panelCardStats = CreateCardPanel("📊 ESTATÍSTICAS", 2)
        Dim statsHost As New Panel()
        statsHost.Dock = DockStyle.Fill
        statsHost.BackColor = Color.FromArgb(45, 45, 48)
        Me.panelCardStats.Controls.Add(statsHost)

        Try
            InitializeFilterControls()
            InitializeSearchControls()
            InitializeStatsControls()

            ' Adicionar cards ao painel principal
            Me.panelStats.Controls.Add(Me.panelCardStats)
            Me.panelStats.Controls.Add(Me.panelCardFilters)
            Me.panelStats.Controls.Add(Me.panelCardSearch)

            ' Mini-mapa vertical das ocorrências
            Me.panelMiniMap = New Panel()
            Me.panelMiniMap.Width = 6
            Me.panelMiniMap.Dock = DockStyle.Right
            Me.panelMiniMap.BackColor = Color.FromArgb(60, 60, 60)
            AddHandler Me.panelMiniMap.Paint, AddressOf DrawMiniMap
            Me.Controls.Add(Me.panelMiniMap)
        Catch ex As Exception
            ' Ignorar erros de inicialização de painéis
        End Try
    End Sub

    Private Function CreateCardPanel(title As String, index As Integer) As Panel
        Dim card As New Panel()
        card.Dock = DockStyle.Top
        card.Height = If(index = 2, 260, If(index = 1, 210, 130)) + 36
        card.BackColor = Color.FromArgb(37, 37, 38)
        card.Padding = New Padding(0, 0, 0, 8)

        Dim header As New Panel()
        header.Dock = DockStyle.Top
        header.Height = 36
        header.BackColor = Color.FromArgb(41, 41, 44)

        Dim lbl As New Label()
        lbl.Text = title
        lbl.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        lbl.ForeColor = Color.White
        lbl.Location = New Point(10, 8)
        lbl.Size = New Size(220, 20)
        header.Controls.Add(lbl)

        Dim badge As New Label()
        badge.AutoSize = False
        badge.TextAlign = ContentAlignment.MiddleCenter
        badge.BackColor = Color.FromArgb(0, 122, 204)
        badge.ForeColor = Color.White
        badge.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        badge.Size = New Size(36, 20)
        badge.Location = New Point(240, 8)
        badge.Text = "0"
        header.Controls.Add(badge)

        If index = 0 Then lblBadgeResults = badge
        If index = 1 Then lblBadgeFilters = badge
        If index = 2 Then lblBadgeTotal = badge

        card.Controls.Add(header)
        Return card
    End Function

    ''' <summary>
    ''' Inicializa controles de filtro
    ''' </summary>
    Private Sub InitializeFilterControls()
        Dim y As Integer = 10

        ' Título
        Dim lblFilters As New Label()
        lblFilters.Text = "🔍 FILTROS"
        lblFilters.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        lblFilters.ForeColor = Color.White
        lblFilters.Location = New Point(10, y)
        lblFilters.Size = New Size(250, 20)
        Me.panelFilters.Controls.Add(lblFilters)
        y += 30

        ' Checkboxes de tipo
        Me.chkInfo = CreateFilterCheckBox("ℹ️ Info", Color.LightGray, 10, y)
        Me.chkWarning = CreateFilterCheckBox("⚠️ Warning", Color.Yellow, 10, y + 25)
        Me.chkError = CreateFilterCheckBox("❌ Error", Color.LightCoral, 10, y + 50)
        Me.chkSuccess = CreateFilterCheckBox("✅ Success", Color.LightGreen, 10, y + 75)
        Me.chkDebug = CreateFilterCheckBox("🔧 Debug", Color.LightBlue, 10, y + 100)

        ' Controles gerais
        Me.chkAutoScroll = CreateFilterCheckBox("📜 Auto Scroll", Color.White, 130, y)
        Me.chkTimestamp = CreateFilterCheckBox("🕒 Timestamp", Color.White, 130, y + 25)

        ' Nível mínimo
        Dim lblNivel As New Label()
        lblNivel.Text = "Nível mínimo:"
        lblNivel.ForeColor = Color.White
        lblNivel.Location = New Point(130, y + 55)
        lblNivel.Size = New Size(100, 20)
        Me.panelFilters.Controls.Add(lblNivel)

        Me.cmbNivelMinimo = New ComboBox()
        Me.cmbNivelMinimo.Items.AddRange({"Debug", "Info", "Warning", "Error"})
        Me.cmbNivelMinimo.SelectedIndex = 0
        Me.cmbNivelMinimo.Location = New Point(130, y + 75)
        Me.cmbNivelMinimo.Size = New Size(120, 25)
        Me.cmbNivelMinimo.DropDownStyle = ComboBoxStyle.DropDownList
        Me.panelFilters.Controls.Add(Me.cmbNivelMinimo)
    End Sub

    ''' <summary>
    ''' Cria checkbox de filtro
    ''' </summary>
    Private Function CreateFilterCheckBox(text As String, color As Color, x As Integer, y As Integer) As CheckBox
        Dim chk As New CheckBox()
        chk.Text = text
        chk.ForeColor = color
        chk.Checked = True
        chk.Location = New Point(x, y)
        chk.Size = New Size(110, 20)
        chk.FlatStyle = FlatStyle.Flat
        Try
            AddHandler chk.CheckedChanged, AddressOf FilterChanged
            Me.panelFilters.Controls.Add(chk)
        Catch ex As Exception
            ' Ignorar erros de event handler
        End Try
        Return chk
    End Function

    ''' <summary>
    ''' Inicializa controles de busca
    ''' </summary>
    Private Sub InitializeSearchControls()
        Try
            Dim y As Integer = 10

            ' Título
            Dim lblSearch As New Label()
            lblSearch.Text = "🔎 BUSCA"
            lblSearch.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            lblSearch.ForeColor = Color.White
            lblSearch.Location = New Point(10, y)
            lblSearch.Size = New Size(250, 20)
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(lblSearch)
            End If
            y += 30

            ' Campo de busca
            Me.txtBusca = New TextBox()
            Me.txtBusca.Location = New Point(10, y)
            Me.txtBusca.Size = New Size(180, 25)
            Me.txtBusca.BackColor = Color.FromArgb(60, 60, 60)
            Me.txtBusca.ForeColor = Color.White
            Me.txtBusca.BorderStyle = BorderStyle.FixedSingle
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(Me.txtBusca)
            End If

            Me.btnBuscar = New Button()
            Me.btnBuscar.Text = "🔍"
            Me.btnBuscar.Location = New Point(195, y)
            Me.btnBuscar.Size = New Size(30, 25)
            Me.btnBuscar.FlatStyle = FlatStyle.Flat
            Me.btnBuscar.BackColor = Color.FromArgb(70, 70, 70)
            Me.btnBuscar.ForeColor = Color.White
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(Me.btnBuscar)
            End If
            y += 35

            ' Navegação
            Me.btnAnterior = New Button()
            Me.btnAnterior.Text = "⬆️ Anterior"
            Me.btnAnterior.Location = New Point(10, y)
            Me.btnAnterior.Size = New Size(80, 25)
            Me.btnAnterior.FlatStyle = FlatStyle.Flat
            Me.btnAnterior.BackColor = Color.FromArgb(70, 70, 70)
            Me.btnAnterior.ForeColor = Color.White
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(Me.btnAnterior)
            End If

            Me.btnProximo = New Button()
            Me.btnProximo.Text = "⬇️ Próximo"
            Me.btnProximo.Location = New Point(95, y)
            Me.btnProximo.Size = New Size(80, 25)
            Me.btnProximo.FlatStyle = FlatStyle.Flat
            Me.btnProximo.BackColor = Color.FromArgb(70, 70, 70)
            Me.btnProximo.ForeColor = Color.White
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(Me.btnProximo)
            End If

            ' Resultados
            Me.lblResultados = New Label()
            Me.lblResultados.Text = "0 resultados"
            Me.lblResultados.ForeColor = Color.LightGray
            Me.lblResultados.Location = New Point(10, y + 30)
            Me.lblResultados.Size = New Size(200, 20)
            If Me.panelSearch IsNot Nothing Then
                Me.panelSearch.Controls.Add(Me.lblResultados)
            End If

        Catch ex As Exception
            ' Ignorar erros de inicialização de busca
        End Try
    End Sub

    ''' <summary>
    ''' Inicializa controles de estatísticas
    ''' </summary>
    Private Sub InitializeStatsControls()
        Dim statsPanel As New Panel()
        statsPanel.Dock = DockStyle.Fill
        statsPanel.BackColor = Color.FromArgb(37, 37, 38)

        Dim y As Integer = 340

        ' Título
        Dim lblStats As New Label()
        lblStats.Text = "📊 ESTATÍSTICAS"
        lblStats.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        lblStats.ForeColor = Color.White
        lblStats.Location = New Point(10, y)
        lblStats.Size = New Size(250, 20)
        statsPanel.Controls.Add(lblStats)
        y += 30

        ' Contadores
        Me.lblTotalInfo = CreateStatsLabel("ℹ️ Info: 0", Color.LightGray, 10, y)
        Me.lblTotalWarning = CreateStatsLabel("⚠️ Warning: 0", Color.Yellow, 10, y + 25)
        Me.lblTotalError = CreateStatsLabel("❌ Error: 0", Color.LightCoral, 10, y + 50)
        Me.lblTotalSuccess = CreateStatsLabel("✅ Success: 0", Color.LightGreen, 10, y + 75)
        Me.lblTotalDebug = CreateStatsLabel("🔧 Debug: 0", Color.LightBlue, 10, y + 100)

        statsPanel.Controls.Add(Me.lblTotalInfo)
        statsPanel.Controls.Add(Me.lblTotalWarning)
        statsPanel.Controls.Add(Me.lblTotalError)
        statsPanel.Controls.Add(Me.lblTotalSuccess)
        statsPanel.Controls.Add(Me.lblTotalDebug)

        ' Memória
        Dim lblMem As New Label()
        lblMem.Text = "💾 Buffer de Memória:"
        lblMem.ForeColor = Color.White
        lblMem.Location = New Point(10, y + 130)
        lblMem.Size = New Size(200, 20)
        statsPanel.Controls.Add(lblMem)

        Me.progressMemoria = New ProgressBar()
        Me.progressMemoria.Location = New Point(10, y + 150)
        Me.progressMemoria.Size = New Size(220, 20)
        Me.progressMemoria.Style = ProgressBarStyle.Continuous
        statsPanel.Controls.Add(Me.progressMemoria)

        Me.panelStats.Controls.Add(statsPanel)
    End Sub

    ''' <summary>
    ''' Cria label de estatística
    ''' </summary>
    Private Function CreateStatsLabel(text As String, color As Color, x As Integer, y As Integer) As Label
        Dim lbl As New Label()
        lbl.Text = text
        lbl.ForeColor = color
        lbl.Location = New Point(x, y)
        lbl.Size = New Size(200, 20)
        lbl.Font = New Font("Segoe UI", 9, FontStyle.Regular)
        Return lbl
    End Function

    ''' <summary>
    ''' Inicializa a status strip
    ''' </summary>
    Private Sub BuildStatusStrip()
        Me.statusStrip = New StatusStrip()
        Me.statusStrip.BackColor = Color.FromArgb(45, 45, 48)
        Me.statusStrip.SizingGrip = False

        Me.lblTotalLinhas = New ToolStripStatusLabel("Linhas: 0")
        Me.lblTotalLinhas.ForeColor = Color.White

        Me.lblFiltrados = New ToolStripStatusLabel("Filtrados: 0")
        Me.lblFiltrados.ForeColor = Color.LightBlue

        Me.lblStatus = New ToolStripStatusLabel("Sistema Ativo")
        Me.lblStatus.ForeColor = Color.LightGreen

        Me.lblUltimaAtualizacao = New ToolStripStatusLabel("Última: Nunca")
        Me.lblUltimaAtualizacao.ForeColor = Color.LightGray

        Me.statusStrip.Items.Add(lblTotalLinhas)
        Me.statusStrip.Items.Add(New ToolStripStatusLabel(" | "))
        Me.statusStrip.Items.Add(lblFiltrados)
        Me.statusStrip.Items.Add(New ToolStripStatusLabel(" | "))
        Me.statusStrip.Items.Add(lblStatus)
        Me.statusStrip.Items.Add(New ToolStripStatusLabel(" | "))
        Me.statusStrip.Items.Add(lblUltimaAtualizacao)
    End Sub

    Private Sub DrawMiniMap(sender As Object, e As PaintEventArgs)
        Try
            Dim g = e.Graphics
            g.Clear(Color.FromArgb(60, 60, 60))
            If currentSearchPositions Is Nothing OrElse currentSearchPositions.Count = 0 Then Return
            
            Dim h As Integer = CType(sender, Control).Height
            Dim totalLines As Integer = Math.Max(1, rtbLogs.Lines.Length)
            
            ' Calcular quantas linhas realmente cabem na área visível do RichTextBox
            Dim lineHeight As Single = rtbLogs.Font.Height
            Dim visibleLinesFloat As Single = rtbLogs.ClientSize.Height / lineHeight
            Dim maxVisibleLines As Integer = Math.Floor(visibleLinesFloat)
            
            ' Determinar a área efetiva que o texto ocupa no minimap
            Dim textAreaRatio As Single = Math.Min(1.0F, maxVisibleLines / totalLines)
            Dim effectiveMinimapHeight As Integer = CInt(h * textAreaRatio)
            
            ' Desenhar uma linha sutil para mostrar onde termina a área do texto
            If effectiveMinimapHeight < h Then
                Using pen As New Pen(Color.FromArgb(100, 100, 100), 1)
                    g.DrawLine(pen, 0, effectiveMinimapHeight, CType(sender, Control).Width, effectiveMinimapHeight)
                End Using
            End If
            
            For i As Integer = 0 To currentSearchPositions.Count - 1
                Dim pos As Integer = currentSearchPositions(i)
                
                ' Obter linha da posição
                Dim lineIndex As Integer = rtbLogs.GetLineFromCharIndex(pos)
                
                ' Calcular posição Y baseada na linha dentro da área efetiva
                Dim y As Integer
                If totalLines <= maxVisibleLines Then
                    ' Se todo o texto cabe, usar proporção direta
                    y = CInt((lineIndex / totalLines) * effectiveMinimapHeight)
                Else
                    ' Se o texto é maior que a área visível, mapear apenas a parte visível
                    Dim firstVisibleLine As Integer = rtbLogs.GetLineFromCharIndex(rtbLogs.GetCharIndexFromPosition(New Point(0, 0)))
                    Dim relativeLineIndex As Integer = lineIndex - firstVisibleLine
                    
                    ' Só desenhar se a linha está na área visível atual
                    If relativeLineIndex >= 0 AndAlso relativeLineIndex < maxVisibleLines Then
                        y = CInt((relativeLineIndex / maxVisibleLines) * effectiveMinimapHeight)
                    Else
                        Continue For ' Pular linhas que não estão visíveis
                    End If
                End If
                
                ' Garantir que não ultrapasse a área efetiva
                y = Math.Max(0, Math.Min(y, effectiveMinimapHeight - 4))
                
                Dim c As Color = If(i = currentSearchIndex, activeHighlightColor, defaultHighlightColor)
                Using br As New SolidBrush(c)
                    g.FillRectangle(br, 0, y, CType(sender, Control).Width, 3)
                End Using
            Next
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Configura layout dos controles
    ''' </summary>
    Private Sub ApplyLayout()
        Me.Controls.Add(Me.rtbLogs)
        Me.Controls.Add(Me.splitterMain)
        Me.Controls.Add(Me.panelStats)
        Me.Controls.Add(Me.panelHeader)
        Me.Controls.Add(Me.mainToolStrip)
        Me.Controls.Add(Me.statusStrip)
    End Sub

    ''' <summary>
    ''' Configura propriedades do formulário
    ''' </summary>
    Private Sub ApplyFormProperties()
        Me.ClientSize = New Size(1200, 700)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.MinimumSize = New Size(900, 600)
        Me.Name = "LogViewerFormProfessional"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Text = "🎯 SUAT-IA - Sistema Profissional de Gestão de Logs"
        Me.BackColor = Color.FromArgb(30, 30, 30)
        ' Atalhos globais
        Me.KeyPreview = True
    End Sub

    ' === MÉTODOS DE FUNCIONALIDADE ===

    ''' <summary>
    ''' Adiciona uma entrada de log ao buffer (método estático)
    ''' </summary>
    Public Shared Sub AddLog(message As String, Optional logType As LogType = LogType.Info)
        Try
            SyncLock _logBuffer
                _logBuffer.Add(New LogEntry(message, logType))

                ' Limitar tamanho do buffer
                If _logBuffer.Count > _maxBufferSize Then
                    _logBuffer.RemoveAt(0)
                End If
            End SyncLock

            ' Atualizar formulário se estiver aberto
            If _instance IsNot Nothing AndAlso Not _instance.IsDisposed AndAlso _instance.Visible Then
                _instance.BeginInvoke(Sub() _instance.RefreshLogs())
            End If

        Catch ex As Exception
            ' Não fazer nada se houver erro no sistema de logs
        End Try
    End Sub

    ''' <summary>
    ''' Carrega logs do buffer para o RichTextBox
    ''' </summary>
    Private Sub LoadBufferedLogs()
        Try
            ApplyFilters()
            RefreshDisplay()
            UpdateStatistics()
        Catch ex As Exception
            rtbLogs.AppendText($"❌ Erro ao carregar logs: {ex.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Aplica filtros aos logs
    ''' </summary>
    Private Sub ApplyFilters()
        Try
            SyncLock _logBuffer
                filteredLogs.Clear()

                For Each entry In _logBuffer
                    If ShouldShowLogEntry(entry) Then
                        filteredLogs.Add(entry)
                    End If
                Next
            End SyncLock
        Catch ex As Exception
            ' Ignorar erros de filtro
        End Try
    End Sub

    ''' <summary>
    ''' Verifica se uma entrada de log deve ser exibida
    ''' </summary>
    Private Function ShouldShowLogEntry(entry As LogEntry) As Boolean
        ' Verificar filtros de tipo
        Select Case entry.LogType
            Case LogType.Info
                If Not chkInfo.Checked Then Return False
            Case LogType.Warning
                If Not chkWarning.Checked Then Return False
            Case LogType.Error
                If Not chkError.Checked Then Return False
            Case LogType.Success
                If Not chkSuccess.Checked Then Return False
            Case LogType.Debug
                If Not chkDebug.Checked Then Return False
        End Select

        ' Verificar nível mínimo
        Dim nivelMinimo = GetLogLevel(cmbNivelMinimo.SelectedIndex)
        If GetLogLevel(entry.LogType) < nivelMinimo Then Return False

        Return True
    End Function

    ''' <summary>
    ''' Converte LogType para nível numérico
    ''' </summary>
    Private Function GetLogLevel(logType As LogType) As Integer
        Select Case logType
            Case LogType.Debug : Return 0
            Case LogType.Info : Return 1
            Case LogType.Warning : Return 2
            Case LogType.Error : Return 3
            Case Else : Return 1
        End Select
    End Function

    ''' <summary>
    ''' Converte índice do combo para nível
    ''' </summary>
    Private Function GetLogLevel(index As Integer) As Integer
        Return index ' Debug=0, Info=1, Warning=2, Error=3
    End Function

    ''' <summary>
    ''' Atualiza apenas novos logs (otimizado)
    ''' </summary>
    Private Sub RefreshLogs()
        Try
            If Not isPaused Then
                ApplyFilters()
                RefreshDisplay()
                UpdateStatistics()
                UpdateStatus()
            End If
        Catch ex As Exception
            ' Ignorar erros de atualização
        End Try
    End Sub

    ''' <summary>
    ''' Atualiza a exibição do RichTextBox
    ''' </summary>
    Private Sub RefreshDisplay()
        Try
            rtbLogs.Clear()

            For Each entry In filteredLogs
                AppendLogEntry(entry)
            Next

            If chkAutoScroll.Checked Then
                ScrollToBottom()
            End If

        Catch ex As Exception
            rtbLogs.AppendText($"❌ Erro na exibição: {ex.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Adiciona uma entrada de log formatada ao RichTextBox
    ''' </summary>
    Private Sub AppendLogEntry(entry As LogEntry)
        Try
            Dim timestamp As String = If(chkTimestamp.Checked, $"[{entry.Timestamp:HH:mm:ss.fff}] ", "")
            Dim fullMessage As String = $"{timestamp}{entry.Message}{Environment.NewLine}"

            ' Definir cor baseada no tipo de log
            Dim color As Color = GetLogColor(entry.LogType)

            ' Adicionar texto colorido
            rtbLogs.SelectionStart = rtbLogs.TextLength
            rtbLogs.SelectionLength = 0
            rtbLogs.SelectionColor = color
            rtbLogs.AppendText(fullMessage)
            rtbLogs.SelectionColor = rtbLogs.ForeColor ' Restaurar cor padrão

        Catch ex As Exception
            ' Fallback para texto simples
            rtbLogs.AppendText($"{entry.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Retorna a cor para o tipo de log
    ''' </summary>
    Private Function GetLogColor(logType As LogType) As Color
        If useVividColors Then
            Select Case logType
                Case LogType.Info
                    Return Color.FromArgb(220, 220, 220) ' branco acinzentado
                Case LogType.Warning
                    Return Color.FromArgb(255, 215, 0) ' gold
                Case LogType.Error
                    Return Color.FromArgb(255, 99, 71) ' tomato
                Case LogType.Success
                    Return Color.FromArgb(50, 205, 50) ' lime green
                Case LogType.Debug
                    Return Color.FromArgb(135, 206, 250) ' light sky blue
                Case Else
                    Return Color.FromArgb(220, 220, 220)
            End Select
        Else
            Select Case logType
                Case LogType.Info
                    Return Color.LightGray
                Case LogType.Warning
                    Return Color.Yellow
                Case LogType.Error
                    Return Color.LightCoral
                Case LogType.Success
                    Return Color.LightGreen
                Case LogType.Debug
                    Return Color.LightBlue
                Case Else
                    Return Color.LightGray
            End Select
        End If
    End Function

    ''' <summary>
    ''' Atualiza informações de status
    ''' </summary>
    Private Sub UpdateStatus()
        Try
            lblTotalLinhas.Text = $"Linhas: {_logBuffer.Count}"
            lblFiltrados.Text = $"Filtrados: {filteredLogs.Count}"
            lblUltimaAtualizacao.Text = $"Última: {DateTime.Now:HH:mm:ss}"
            lblStatus.Text = If(isPaused, "⏸️ Pausado", "▶️ Ativo")

            ' Atualizar barra de memória
            progressMemoria.Value = Math.Min(100, (_logBuffer.Count * 100) \ _maxBufferSize)

        Catch ex As Exception
            ' Ignorar erros de status
        End Try
    End Sub

    ''' <summary>
    ''' Atualiza estatísticas por tipo
    ''' </summary>
    Private Sub UpdateStatistics()
        Try
            Dim stats(4) As Integer ' Debug, Info, Warning, Error, Success

            SyncLock _logBuffer
                For Each entry In _logBuffer
                    Select Case entry.LogType
                        Case LogType.Debug : stats(0) += 1
                        Case LogType.Info : stats(1) += 1
                        Case LogType.Warning : stats(2) += 1
                        Case LogType.Error : stats(3) += 1
                        Case LogType.Success : stats(4) += 1
                    End Select
                Next
            End SyncLock

            lblTotalDebug.Text = $"🔧 Debug: {stats(0)}"
            lblTotalInfo.Text = $"ℹ️ Info: {stats(1)}"
            lblTotalWarning.Text = $"⚠️ Warning: {stats(2)}"
            lblTotalError.Text = $"❌ Error: {stats(3)}"
            lblTotalSuccess.Text = $"✅ Success: {stats(4)}"
            If lblBadgeTotal IsNot Nothing Then lblBadgeTotal.Text = _logBuffer.Count.ToString()

        Catch ex As Exception
            ' Ignorar erros de estatísticas
        End Try
    End Sub

    ''' <summary>
    ''' Rola para o final do log
    ''' </summary>
    Private Sub ScrollToBottom()
        Try
            rtbLogs.SelectionStart = rtbLogs.Text.Length
            rtbLogs.ScrollToCaret()
        Catch ex As Exception
            ' Ignorar erros de scroll
        End Try
    End Sub

    ' === EVENTOS ===

    Private Sub FilterChanged(sender As Object, e As EventArgs)
        RefreshLogs()
    End Sub

    Private Sub btnLimpar_Click(sender As Object, e As EventArgs) Handles btnLimpar.Click
        Try
            If MessageBox.Show("Deseja realmente limpar todos os logs?", "Confirmar",
                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then

                SyncLock _logBuffer
                    _logBuffer.Clear()
                End SyncLock

                filteredLogs.Clear()
                rtbLogs.Clear()
                lastDisplayedCount = 0
                UpdateStatus()
                UpdateStatistics()

                AddLog("🗑️ Logs limpos pelo usuário", LogType.Info)
            End If
        Catch ex As Exception
            MessageBox.Show($"Erro ao limpar logs: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnExportar_Click(sender As Object, e As EventArgs) Handles btnExportar.Click
        Try
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Arquivo de Log (*.log)|*.log|Arquivo de Texto (*.txt)|*.txt|CSV (*.csv)|*.csv"
                sfd.FileName = $"SUAT_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.log"

                If sfd.ShowDialog() = DialogResult.OK Then
                    ExportLogs(sfd.FileName)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Erro ao exportar logs: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportLogs(fileName As String)
        Dim logContent As New StringBuilder()

        SyncLock _logBuffer
            For Each entry In _logBuffer
                logContent.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.LogType}] {entry.Message}")
            Next
        End SyncLock

        File.WriteAllText(fileName, logContent.ToString(), Encoding.UTF8)

        MessageBox.Show($"Logs exportados para: {fileName}", "Exportação Concluída",
                      MessageBoxButtons.OK, MessageBoxIcon.Information)

        AddLog($"💾 Logs exportados para: {fileName}", LogType.Success)
    End Sub

    Private Sub btnPausar_Click(sender As Object, e As EventArgs) Handles btnPausar.Click
        isPaused = Not isPaused
        btnPausar.Text = If(isPaused, "▶️ Retomar", "⏸️ Pausar")
        UpdateStatus()
        AddLog(If(isPaused, "⏸️ Captura pausada", "▶️ Captura retomada"), LogType.Info)
    End Sub

    Private Sub btnEstatisticas_Click(sender As Object, e As EventArgs) Handles btnEstatisticas.Click
        showStatsPanel = Not showStatsPanel
        panelStats.Visible = showStatsPanel
        splitterMain.Visible = showStatsPanel
    End Sub

    Private Sub btnDrawer_Click(sender As Object, e As EventArgs) Handles btnDrawer.Click
        showStatsPanel = Not showStatsPanel
        panelStats.Visible = showStatsPanel
        splitterMain.Visible = showStatsPanel
        btnDrawer.Text = If(showStatsPanel, "📥 Drawer", "📤 Drawer")
    End Sub

    Private Sub btnConfiguracoes_Click(sender As Object, e As EventArgs) Handles btnConfiguracoes.Click
        Try
            Dim menu As New ContextMenuStrip()
            menu.BackColor = Color.FromArgb(45, 45, 48)
            menu.ForeColor = Color.White
            Dim itmCores As New ToolStripMenuItem(If(useVividColors, "Desativar cores vivas", "Ativar cores vivas"))
            AddHandler itmCores.Click, Sub()
                                           useVividColors = Not useVividColors
                                           RefreshLogs()
                                       End Sub
            Dim itmWrap As New ToolStripMenuItem(If(rtbLogs.WordWrap, "Desativar quebra de linha", "Ativar quebra de linha"))
            AddHandler itmWrap.Click, Sub()
                                          rtbLogs.WordWrap = Not rtbLogs.WordWrap
                                      End Sub
            Dim itmFonteMaior As New ToolStripMenuItem("Aumentar fonte")
            AddHandler itmFonteMaior.Click, Sub()
                                                rtbLogs.Font = New Font(rtbLogs.Font.FontFamily, Math.Min(20.0F, rtbLogs.Font.Size + 1.0F), rtbLogs.Font.Style)
                                            End Sub
            Dim itmFonteMenor As New ToolStripMenuItem("Diminuir fonte")
            AddHandler itmFonteMenor.Click, Sub()
                                                rtbLogs.Font = New Font(rtbLogs.Font.FontFamily, Math.Max(8.0F, rtbLogs.Font.Size - 1.0F), rtbLogs.Font.Style)
                                            End Sub
            menu.Items.AddRange(New ToolStripItem() {itmCores, itmWrap, New ToolStripSeparator(), itmFonteMaior, itmFonteMenor})
            menu.Show(Me, PointToClient(Cursor.Position))
        Catch
        End Try
    End Sub

    Private Sub btnBuscar_Click(sender As Object, e As EventArgs) Handles btnBuscar.Click
        PerformSearch()
    End Sub

    Private Sub txtBusca_KeyDown(sender As Object, e As KeyEventArgs) Handles txtBusca.KeyDown
        If e.KeyCode = Keys.Enter Then
            PerformSearch()
        End If
    End Sub

    Private Sub PerformSearch()
        Try
            ' Limpar highlights anteriores
            ' Limpar highlights anteriores
            Me.rtbLogs.Select(0, 0)
            rtbLogs.SelectionBackColor = Color.FromArgb(30, 30, 30)
            Me.rtbLogs.SelectAll()
            Me.rtbLogs.SelectionBackColor = Color.FromArgb(30, 30, 30)
            Me.rtbLogs.DeselectAll()

            currentSearchPositions.Clear()
            currentSearchIndex = -1

            If String.IsNullOrEmpty(txtBusca.Text) Then
                lblResultados.Text = "0 resultados"
                Return
            End If

            Dim searchText = txtBusca.Text
            lastSearchText = searchText
            Dim text As String = rtbLogs.Text
            Dim idx As Integer = 0
            While idx < text.Length
                Dim found As Integer = text.IndexOf(searchText, idx, StringComparison.OrdinalIgnoreCase)
                If found = -1 Then Exit While
                currentSearchPositions.Add(found)
                ' Realçar ocorrência 
                rtbLogs.Select(found, searchText.Length)
                rtbLogs.SelectionBackColor = defaultHighlightColor
                idx = found + searchText.Length
            End While

            lblResultados.Text = $"{currentSearchPositions.Count} resultados"
            If lblBadgeResults IsNot Nothing Then lblBadgeResults.Text = currentSearchPositions.Count.ToString()

            If currentSearchPositions.Count > 0 Then
                currentSearchIndex = 0
                HighlightSearchResult()
            End If

        Catch ex As Exception
            lblResultados.Text = "Erro na busca"
        End Try
    End Sub

    Private Sub btnProximo_Click(sender As Object, e As EventArgs) Handles btnProximo.Click
        If currentSearchPositions.Count > 0 Then
            currentSearchIndex = (currentSearchIndex + 1) Mod currentSearchPositions.Count
            HighlightSearchResult()
        End If
    End Sub

    Private Sub btnAnterior_Click(sender As Object, e As EventArgs) Handles btnAnterior.Click
        If currentSearchPositions.Count > 0 Then
            currentSearchIndex = If(currentSearchIndex - 1 < 0, currentSearchPositions.Count - 1, currentSearchIndex - 1)
            HighlightSearchResult()
        End If
    End Sub

    Private Sub HighlightSearchResult()
        Try
            If currentSearchIndex >= 0 AndAlso currentSearchIndex < currentSearchPositions.Count Then
                Dim startPos = currentSearchPositions(currentSearchIndex)
                If startPos >= 0 AndAlso startPos < rtbLogs.TextLength Then
                    ' Restaurar cor da ocorrência anterior para o azul padrão
                    If previousActiveIndex >= 0 AndAlso previousActiveIndex < currentSearchPositions.Count AndAlso previousActiveIndex <> currentSearchIndex Then
                        Dim prevPos = currentSearchPositions(previousActiveIndex)
                        rtbLogs.Select(prevPos, Math.Max(1, lastSearchText.Length))
                        rtbLogs.SelectionBackColor = defaultHighlightColor
                    End If

                    ' Destacar linha inteira da ocorrência atual com cinza escuro
                    Dim lineIdx As Integer = rtbLogs.GetLineFromCharIndex(startPos)
                    Dim lineStart As Integer = rtbLogs.GetFirstCharIndexFromLine(lineIdx)
                    Dim lineText As String = rtbLogs.Lines(lineIdx)
                    rtbLogs.Select(lineStart, lineText.Length)
                    rtbLogs.SelectionBackColor = lineHighlightColor

                    ' Aplicar destaque âmbar somente na palavra ativa
                    rtbLogs.Select(startPos, Math.Max(1, lastSearchText.Length))
                    rtbLogs.SelectionBackColor = activeHighlightColor

                    rtbLogs.ScrollToCaret()
                    panelMiniMap.Invalidate()
                    previousActiveIndex = currentSearchIndex
                    lblResultados.Text = $"{currentSearchIndex + 1} de {currentSearchPositions.Count} resultados"
                End If
            End If
        Catch ex As Exception
            ' Ignorar erros de highlight
        End Try
    End Sub

    Private Sub btnFechar_Click(sender As Object, e As EventArgs) Handles btnFechar.Click
        Me.Hide()
    End Sub

    ' --- Handlers do Context Menu ---
    Private Sub menuCopy_Click(sender As Object, e As EventArgs) Handles menuCopy.Click
        Try
            If Not String.IsNullOrEmpty(rtbLogs.SelectedText) Then
                Clipboard.SetText(rtbLogs.SelectedText)
            End If
        Catch
        End Try
    End Sub

    Private Sub menuCopyAll_Click(sender As Object, e As EventArgs) Handles menuCopyAll.Click
        Try
            Clipboard.SetText(rtbLogs.Text)
        Catch
        End Try
    End Sub

    Private Sub menuSaveSelection_Click(sender As Object, e As EventArgs) Handles menuSaveSelection.Click
        Try
            If String.IsNullOrEmpty(rtbLogs.SelectedText) Then Return
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Texto (*.txt)|*.txt|Log (*.log)|*.log"
                sfd.FileName = $"SUAT_Selecao_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                If sfd.ShowDialog() = DialogResult.OK Then
                    File.WriteAllText(sfd.FileName, rtbLogs.SelectedText, Encoding.UTF8)
                End If
            End Using
        Catch
        End Try
    End Sub

    Private Sub menuSaveAll_Click(sender As Object, e As EventArgs) Handles menuSaveAll.Click
        Try
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Texto (*.txt)|*.txt|Log (*.log)|*.log"
                sfd.FileName = $"SUAT_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                If sfd.ShowDialog() = DialogResult.OK Then
                    File.WriteAllText(sfd.FileName, rtbLogs.Text, Encoding.UTF8)
                End If
            End Using
        Catch
        End Try
    End Sub

    Private Sub menuClear_Click(sender As Object, e As EventArgs) Handles menuClear.Click
        btnLimpar_Click(sender, e)
    End Sub

    Private Sub menuToggleWrap_Click(sender As Object, e As EventArgs) Handles menuToggleWrap.Click
        rtbLogs.WordWrap = Not rtbLogs.WordWrap
    End Sub

    Private Sub menuIncreaseFont_Click(sender As Object, e As EventArgs) Handles menuIncreaseFont.Click
        rtbLogs.Font = New Font(rtbLogs.Font.FontFamily, Math.Min(20.0F, rtbLogs.Font.Size + 1.0F), rtbLogs.Font.Style)
    End Sub

    Private Sub menuDecreaseFont_Click(sender As Object, e As EventArgs) Handles menuDecreaseFont.Click
        rtbLogs.Font = New Font(rtbLogs.Font.FontFamily, Math.Max(8.0F, rtbLogs.Font.Size - 1.0F), rtbLogs.Font.Style)
    End Sub

    ''' <summary>
    ''' Previne fechamento real do formulário
    ''' </summary>
    Private Sub LogViewerForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
        End If
    End Sub

    ''' <summary>
    ''' Intercepta o fechamento para apenas ocultar o formulário
    ''' </summary>
    Protected Overrides Sub SetVisibleCore(value As Boolean)
        MyBase.SetVisibleCore(value)
        If value Then
            RefreshLogs() ' Atualizar logs ao mostrar
        End If
    End Sub

    ''' <summary>
    ''' Atalhos de teclado globais
    ''' </summary>
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.Control AndAlso e.KeyCode = Keys.F Then
            If Me.txtBusca IsNot Nothing Then Me.txtBusca.Focus()
        ElseIf e.KeyCode = Keys.F3 Then
            btnProximo_Click(Me, EventArgs.Empty)
        ElseIf e.Control AndAlso e.KeyCode = Keys.S Then
            btnExportar_Click(Me, EventArgs.Empty)
        End If
    End Sub

End Class
