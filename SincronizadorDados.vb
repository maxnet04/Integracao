Imports System.Data.SQLite
Imports System.IO
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Classe responsável pela sincronização de dados usando dados fixos para teste
''' Compatível com .NET Framework 4.7
''' Implementa o plano de execução para nova instalação
''' </summary>
Public Class SincronizadorDados
    Private ReadOnly dbManager As DatabaseManager
    Private Const LOCAL_VERSION_FILE As String = "version.local"
    Private Const INSTALLATION_FLAG_FILE As String = "installation.flag"
    Private Const DATABASE_INIT_FILE As String = "database.init"
    
    Public Sub New(Optional dbPath As String = Nothing)
        dbManager = New DatabaseManager(dbPath)
    End Sub
    
    ''' <summary>
    ''' Detecta se é uma nova instalação baseado em múltiplos indicadores
    ''' </summary>
    ''' <returns>True se for nova instalação, False se for atualização</returns>
    Public Function IsNovaInstalacao() As Boolean
        Console.WriteLine("🔍 Verificando se é uma nova instalação...")
        
        Dim indicadores As New List(Of String)
        
        ' 1. Verificar se o arquivo de versão local existe
        Dim versionFile = Path.Combine(Application.StartupPath, LOCAL_VERSION_FILE)
        If Not File.Exists(versionFile) Then
            indicadores.Add("Arquivo version.local não encontrado")
        End If
        
        ' 2. Verificar se o arquivo de flag de instalação existe
        If Not File.Exists(INSTALLATION_FLAG_FILE) Then
            indicadores.Add("Arquivo installation.flag não encontrado")
        End If
        
        ' 3. Verificar se o banco de dados existe e tem dados
        Dim bancoExiste = dbManager.VerificarExistenciaBanco()
        If Not bancoExiste Then
            indicadores.Add("Banco de dados não encontrado")
        Else
            ' Verificar se o banco tem dados
            Dim resultado = dbManager.ExecutarQuery("SELECT COUNT(*) as total FROM incidents")
            If resultado.Success AndAlso resultado.Registros.Count > 0 Then
                Dim totalIncidentes = Convert.ToInt32(resultado.Registros(0)("total"))
                If totalIncidentes = 0 Then
                    indicadores.Add("Banco de dados vazio (sem incidentes)")
                End If
            Else
                indicadores.Add("Não foi possível verificar dados do banco")
            End If
        End If
        
        ' 4. Verificar se a tabela historical_data tem dados
        If bancoExiste Then
            Dim resultadoHistorical = dbManager.ExecutarQuery("SELECT COUNT(*) as total FROM historical_data")
            If resultadoHistorical.Success AndAlso resultadoHistorical.Registros.Count > 0 Then
                Dim totalHistorical = Convert.ToInt32(resultadoHistorical.Registros(0)("total"))
                If totalHistorical = 0 Then
                    indicadores.Add("Tabela historical_data vazia")
                End If
            Else
                indicadores.Add("Tabela historical_data não encontrada ou inacessível")
            End If
        End If
        
        ' 5. Verificar se o arquivo de inicialização do banco existe
        If Not File.Exists(DATABASE_INIT_FILE) Then
            indicadores.Add("Arquivo database.init não encontrado")
        End If
        
        ' Decisão baseada nos indicadores
        Dim resultadoNovaInstalacao = indicadores.Count >= 3 ' Se pelo menos 3 indicadores apontam para nova instalação
        
        Console.WriteLine($"📊 Indicadores de nova instalação encontrados: {indicadores.Count}")
        For Each indicador In indicadores
            Console.WriteLine($"   ⚠️ {indicador}")
        Next
        
        If resultadoNovaInstalacao Then
            Console.WriteLine("✅ Detectada NOVA INSTALAÇÃO")
        Else
            Console.WriteLine("✅ Detectada ATUALIZAÇÃO/EXECUÇÃO NORMAL")
        End If
        
        Return resultadoNovaInstalacao
    End Function
    
    ''' <summary>
    ''' Executa sincronização inteligente que decide automaticamente entre carga inicial ou incremental
    ''' </summary>
    Public Sub ExecutarSincronizacaoInteligente()
        Console.WriteLine("🚀 Iniciando sincronização inteligente...")
        
        Try
            If IsNovaInstalacao() Then
                Console.WriteLine("🆕 Executando carga inicial para nova instalação...")
                RealizarCargaInicial()
                MarcarComoInstalado()
            Else
                Console.WriteLine("🔄 Executando carga incremental para instalação existente...")
                RealizarCargaIncremental()
            End If
            
            Console.WriteLine("✅ Sincronização inteligente concluída!")
            
        Catch ex As Exception
            Console.WriteLine($"❌ Erro na sincronização inteligente: {ex.Message}")
            Throw
        End Try
    End Sub
    
    ''' <summary>
    ''' Marca o sistema como instalado criando os arquivos de controle necessários
    ''' </summary>
    Private Sub MarcarComoInstalado()
        Try
            Console.WriteLine("🏷️ Marcando sistema como instalado...")
            
            ' 1. Criar arquivo de flag de instalação
            File.WriteAllText(INSTALLATION_FLAG_FILE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            Console.WriteLine($"   ✅ Criado: {INSTALLATION_FLAG_FILE}")
            
            ' 2. Criar arquivo de inicialização do banco
            File.WriteAllText(DATABASE_INIT_FILE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            Console.WriteLine($"   ✅ Criado: {DATABASE_INIT_FILE}")
            
            ' 3. Criar version.local com versão apropriada
            Dim versionFile = Path.Combine(Application.StartupPath, LOCAL_VERSION_FILE)
            If Not File.Exists(versionFile) Then
                ' Tentar obter versão da aplicação atual, senão usar padrão
                Dim versaoAtual = ObterVersaoAplicacao()
                File.WriteAllText(versionFile, versaoAtual)
                Console.WriteLine($"   ✅ Criado: {versionFile} com versão {versaoAtual}")
            Else
                Console.WriteLine($"   ℹ️ Arquivo {versionFile} já existe")
            End If
            
            Console.WriteLine("✅ Sistema marcado como instalado com sucesso!")
            
        Catch ex As Exception
            Console.WriteLine($"⚠️ Erro ao marcar como instalado: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Obtém a versão da aplicação atual
    ''' </summary>
    Private Function ObterVersaoAplicacao() As String
        Try
            ' Tentar obter da versão do assembly
            Dim version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            If version IsNot Nothing Then
                Return $"{version.Major}.{version.Minor}.{version.Build}"
            End If
        Catch ex As Exception
            Console.WriteLine($"   ⚠️ Erro ao obter versão do assembly: {ex.Message}")
        End Try
        
        ' Versão padrão se não conseguir obter
        Return "1.0.0"
    End Function
    
    ''' <summary>
    ''' Executa carga inicial completa dos dados (últimos 3 anos simulados)
    ''' Usado na primeira instalação
    ''' </summary>
    Public Sub RealizarCargaInicial()
        Console.WriteLine("🔄 Iniciando carga inicial de dados (últimos 3 anos)...")
        
        Try
            ' Conectar ao banco
            Dim conexaoResult = dbManager.Conectar()
            If Not conexaoResult.Success Then
                Console.WriteLine($"❌ Erro ao conectar: {conexaoResult.Mensagem}")
                Return
            End If
            
            Console.WriteLine($"✅ Conectado ao banco: {conexaoResult.TotalTabelas} tabelas encontradas")
            
            ' Limpar dados existentes para carga completa
            Console.WriteLine("🧹 Limpando dados existentes para carga inicial...")
            LimparDadosExistentes()
            
            ' Inserir dados históricos (últimos 3 anos)
            Console.WriteLine("📝 Inserindo dados históricos (últimos 3 anos)...")
            InserirDadosHistoricosCompletos()
            
            ' Atualizar dados históricos agregados
            Console.WriteLine("📊 Atualizando dados históricos agregados...")
            AtualizarDadosHistoricos()
            
            Console.WriteLine("✅ Carga inicial concluída!")
            
        Catch ex As Exception
            Console.WriteLine($"❌ Erro na carga inicial: {ex.Message}")
            Throw
        Finally
            ' Desconectar do banco
            dbManager.Desconectar()
        End Try
    End Sub
    
    ''' <summary>
    ''' Executa carga incremental de dados (apenas dados novos)
    ''' Usado nas execuções subsequentes
    ''' </summary>
    Public Sub RealizarCargaIncremental()
        Console.WriteLine("🔄 Iniciando carga incremental de dados...")
        
        Try
            ' Conectar ao banco
            Dim conexaoResult = dbManager.Conectar()
            If Not conexaoResult.Success Then
                Console.WriteLine($"❌ Erro ao conectar: {conexaoResult.Mensagem}")
                Return
            End If
            
            Console.WriteLine($"✅ Conectado ao banco: {conexaoResult.TotalTabelas} tabelas encontradas")
            
            ' Obter data do último registro
            Dim ultimaData = ObterDataUltimoRegistro()
            Console.WriteLine($"📅 Última data sincronizada: {ultimaData:dd/MM/yyyy}")
            
            ' Inserir apenas dados novos (últimos 7 dias simulados)
            Console.WriteLine("📝 Inserindo dados incrementais (últimos 7 dias)...")
            InserirDadosIncrementais(ultimaData)
            
            ' Atualizar dados históricos agregados
            Console.WriteLine("📊 Atualizando dados históricos agregados...")
            AtualizarDadosHistoricos()
            
            Console.WriteLine("✅ Carga incremental concluída!")
            
        Catch ex As Exception
            Console.WriteLine($"❌ Erro na carga incremental: {ex.Message}")
            Throw
        Finally
            ' Desconectar do banco
            dbManager.Desconectar()
        End Try
    End Sub
    
    ''' <summary>
    ''' Executa sincronização de teste com dados fixos (método original mantido para compatibilidade)
    ''' </summary>
    Public Sub ExecutarSincronizacaoTeste()
        Console.WriteLine("🔄 Iniciando sincronização de teste...")
        
        Try
            ' Conectar ao banco
            Dim conexaoResult = dbManager.Conectar()
            If Not conexaoResult.Success Then
                Console.WriteLine($"❌ Erro ao conectar: {conexaoResult.Mensagem}")
                Return
            End If
            
            Console.WriteLine($"✅ Conectado ao banco: {conexaoResult.TotalTabelas} tabelas encontradas")
            
            ' Limpar dados existentes (opcional)
            Console.WriteLine("🧹 Limpando dados existentes...")
            LimparDadosExistentes()
            
            ' Inserir dados de teste
            Console.WriteLine("📝 Inserindo dados de teste...")
            InserirDadosTeste()
            
            ' Atualizar dados históricos
            Console.WriteLine("📊 Atualizando dados históricos...")
            AtualizarDadosHistoricos()
            
            Console.WriteLine("✅ Sincronização de teste concluída!")
            
        Catch ex As Exception
            Console.WriteLine($"❌ Erro na sincronização: {ex.Message}")
            Throw
        Finally
            ' Desconectar do banco
            dbManager.Desconectar()
        End Try
    End Sub
    
    ''' <summary>
    ''' Insere dados históricos completos simulando 3 anos de dados
    ''' </summary>
    Private Sub InserirDadosHistoricosCompletos()
        Dim registrosInseridos As Integer = 0
        Dim registrosComErro As Integer = 0
        
        ' Gerar dados para os últimos 3 anos (aproximadamente 1000 incidentes)
        Dim dataInicio = DateTime.Now.AddYears(-3)
        Dim dataFim = DateTime.Now
        
        ' Grupos de suporte
        Dim grupos As String() = {"Suporte Técnico", "Desenvolvimento", "Infraestrutura", "Segurança", "DBA"}
        Dim categorias As String() = {"Acesso", "Hardware", "Software", "Rede", "Email", "Backup", "Segurança", "Performance"}
        Dim prioridades As String() = {"Baixa", "Média", "Alta", "Crítica"}
        Dim departamentos As String() = {"TI", "Administrativo", "Financeiro", "Vendas", "Marketing", "RH", "Operações"}
        
        Dim random As New Random(42) ' Seed fixo para reprodutibilidade
        
        ' Gerar aproximadamente 1000 incidentes distribuídos nos 3 anos
        For i As Integer = 1 To 1000
            Try
                ' Data aleatória nos últimos 3 anos
                Dim diasAleatorios = random.Next(0, (dataFim - dataInicio).Days)
                Dim dataCriacao = dataInicio.AddDays(diasAleatorios)
                
                ' 80% dos incidentes têm data de encerramento
                Dim dataEncerramento As DateTime? = Nothing
                If random.Next(100) < 80 Then
                    Dim tempoResolucao = random.Next(1, 30) ' 1 a 30 dias
                    dataEncerramento = dataCriacao.AddDays(tempoResolucao)
                End If
                
                Dim incidente As New IncidenteTeste With {
                    .Id = $"INC{dataCriacao:yyyyMMdd}{i:D3}",
                    .Assunto = GerarAssuntoAleatorio(random, categorias(random.Next(categorias.Length))),
                    .Departamento = departamentos(random.Next(departamentos.Length)),
                    .GrupoDirecionado = grupos(random.Next(grupos.Length)),
                    .Categoria = categorias(random.Next(categorias.Length)),
                    .Prioridade = prioridades(random.Next(prioridades.Length)),
                    .DataCriacao = dataCriacao,
                    .DataEncerramento = dataEncerramento
                }

                ' Usar o método do DatabaseManager que recebe os campos individuais
                Dim resultado = dbManager.InserirIncidente(
                    incidente.Id,
                    incidente.Assunto,
                    incidente.Departamento,
                    incidente.GrupoDirecionado,
                    incidente.Categoria,
                    incidente.Prioridade,
                    incidente.DataCriacao,
                    incidente.DataEncerramento)
                If resultado.Success Then
                    registrosInseridos += 1

                    ' Mostrar progresso a cada 100 registros
                    If registrosInseridos Mod 100 = 0 Then
                        Console.WriteLine($"   📊 Progresso: {registrosInseridos} registros inseridos...")
                    End If
                Else
                    registrosComErro += 1
                    Console.WriteLine($"   ⚠️ Erro ao inserir incidente {incidente.Id}: {resultado.Mensagem}")
                End If
            Catch ex As Exception
                registrosComErro += 1
                Console.WriteLine($"   ⚠️ Exceção ao inserir incidente {i}: {ex.Message}")
            End Try
        Next

        Console.WriteLine($"   ✅ Inseridos {registrosInseridos} incidentes históricos")
        If registrosComErro > 0 Then
            Console.WriteLine($"   ⚠️ {registrosComErro} incidentes com erro")
        End If
    End Sub

    ''' <summary>
    ''' Insere dados incrementais (apenas dados novos desde a última sincronização)
    ''' </summary>
    Private Sub InserirDadosIncrementais(ultimaData As DateTime)
        Dim registrosInseridos As Integer = 0
        Dim registrosComErro As Integer = 0

        ' Verificar se a última data é futura ou muito antiga
        Dim dataFim = DateTime.Now
        Dim dataInicio As DateTime

        If ultimaData > dataFim Then
            ' Se a última data é futura, usar os últimos 7 dias
            Console.WriteLine($"   ⚠️ Última data ({ultimaData:dd/MM/yyyy}) é futura. Usando últimos 7 dias.")
            dataInicio = dataFim.AddDays(-7)
        Else
            ' Usar a data após a última sincronização
            dataInicio = ultimaData.AddDays(1)
        End If

        ' Verificar se há período válido para gerar dados
        Dim diasDisponiveis = (dataFim - dataInicio).Days
        If diasDisponiveis <= 0 Then
            Console.WriteLine($"   ℹ️ Nenhum período válido para dados incrementais (dias: {diasDisponiveis})")
            Return
        End If

        ' Limitar o período máximo para evitar overflow
        Dim periodoMaximo = Math.Min(diasDisponiveis, 30) ' Máximo 30 dias
        If diasDisponiveis > 30 Then
            Console.WriteLine($"   ⚠️ Período muito longo ({diasDisponiveis} dias). Limitando a 30 dias.")
            dataInicio = dataFim.AddDays(-30)
        End If

        Console.WriteLine($"   📅 Gerando dados de {dataInicio:dd/MM/yyyy} até {dataFim:dd/MM/yyyy} ({periodoMaximo} dias)")

        ' Grupos de suporte
        Dim grupos As String() = {"Suporte Técnico", "Desenvolvimento", "Infraestrutura", "Segurança", "DBA"}
        Dim categorias As String() = {"Acesso", "Hardware", "Software", "Rede", "Email", "Backup", "Segurança", "Performance"}
        Dim prioridades As String() = {"Baixa", "Média", "Alta", "Crítica"}
        Dim departamentos As String() = {"TI", "Administrativo", "Financeiro", "Vendas", "Marketing", "RH", "Operações"}

        Dim random As New Random(CInt(DateTime.Now.Ticks Mod Integer.MaxValue)) ' Seed baseado no tempo atual (modificado para evitar overflow)

        ' Gerar aproximadamente 50 incidentes no período válido
        For i As Integer = 1 To 50
            Try
                ' Data aleatória no período válido (usando período limitado)
                Dim diasAleatorios = random.Next(0, periodoMaximo + 1)
                Dim dataCriacao = dataInicio.AddDays(diasAleatorios)

                ' 70% dos incidentes têm data de encerramento (mais realista para dados recentes)
                Dim dataEncerramento As DateTime? = Nothing
                If random.Next(100) < 70 Then
                    Dim tempoResolucao = random.Next(1, 7) ' 1 a 7 dias
                    dataEncerramento = dataCriacao.AddDays(tempoResolucao)
                End If

                Dim incidente As New IncidenteTeste With {
                    .Id = $"INC{dataCriacao:yyyyMMdd}{i:D3}",
                    .Assunto = GerarAssuntoAleatorio(random, categorias(random.Next(categorias.Length))),
                    .Departamento = departamentos(random.Next(departamentos.Length)),
                    .GrupoDirecionado = grupos(random.Next(grupos.Length)),
                    .Categoria = categorias(random.Next(categorias.Length)),
                    .Prioridade = prioridades(random.Next(prioridades.Length)),
                    .DataCriacao = dataCriacao,
                    .DataEncerramento = dataEncerramento
                }

                ' Usar o método do DatabaseManager que recebe os campos individuais
                Dim resultado = dbManager.InserirIncidente(
                    incidente.Id,
                    incidente.Assunto,
                    incidente.Departamento,
                    incidente.GrupoDirecionado,
                    incidente.Categoria,
                    incidente.Prioridade,
                    incidente.DataCriacao,
                    incidente.DataEncerramento)
                If resultado.Success Then
                    registrosInseridos += 1
                Else
                    registrosComErro += 1
                    Console.WriteLine($"   ⚠️ Erro ao inserir incidente {incidente.Id}: {resultado.Mensagem}")
                End If
            Catch ex As Exception
                registrosComErro += 1
                Console.WriteLine($"   ⚠️ Exceção ao inserir incidente {i}: {ex.Message}")
            End Try
        Next

        Console.WriteLine($"   ✅ Inseridos {registrosInseridos} incidentes incrementais")
        If registrosComErro > 0 Then
            Console.WriteLine($"   ⚠️ {registrosComErro} incidentes com erro")
        End If
    End Sub

    ''' <summary>
    ''' Gera assuntos aleatórios baseados na categoria
    ''' </summary>
    Private Function GerarAssuntoAleatorio(random As Random, categoria As String) As String
        Dim assuntos As New Dictionary(Of String, String()) From {
            {"Acesso", {"Usuário não consegue fazer login", "Senha expirada", "Acesso negado ao sistema", "Problema de autenticação", "Usuário bloqueado"}},
            {"Hardware", {"Impressora não funciona", "Mouse quebrado", "Teclado com teclas travadas", "Monitor com problema", "CPU superaquecendo"}},
            {"Software", {"Erro no relatório mensal", "Sistema lento", "Aplicação não abre", "Erro de compatibilidade", "Atualização necessária"}},
            {"Rede", {"Internet lenta", "Conexão instável", "WiFi não conecta", "Cabo de rede com problema", "Switch com falha"}},
            {"Email", {"Email não chega", "Spam excessivo", "Anexo não abre", "Caixa de entrada cheia", "Configuração de email"}},
            {"Backup", {"Backup não realizado", "Restauração falhou", "Arquivo corrompido", "Espaço insuficiente", "Agendamento não funciona"}},
            {"Segurança", {"Vírus detectado", "Firewall bloqueando", "Acesso não autorizado", "Dados vazados", "Certificado expirado"}},
            {"Performance", {"Sistema muito lento", "Timeout em consultas", "Memória insuficiente", "CPU em 100%", "Disco cheio"}}
        }

        If assuntos.ContainsKey(categoria) Then
            Return assuntos(categoria)(random.Next(assuntos(categoria).Length))
        Else
            Return "Problema técnico geral"
        End If
    End Function

    ''' <summary>
    ''' Obtém a data do último incidente registrado no banco de dados local
    ''' </summary>
    Private Function ObterDataUltimoRegistro() As DateTime
        Try
            Dim resultado = dbManager.ExecutarQuery("SELECT MAX(DATA_CRIACAO) as ultima_data FROM incidents")
            If resultado.Success AndAlso resultado.Registros.Count > 0 Then
                Dim ultimaData = resultado.Registros(0)("ultima_data")
                If ultimaData IsNot Nothing AndAlso Not IsDBNull(ultimaData) Then
                    Dim data = DateTime.Parse(ultimaData.ToString())

                    ' Verificar se a data é válida e não é futura
                    If data > DateTime.Now Then
                        Console.WriteLine($"   ⚠️ Data futura encontrada no banco: {data:dd/MM/yyyy}. Usando data atual.")
                        Return DateTime.Now.AddDays(-1)
                    End If

                    Return data
                End If
            End If
            Return DateTime.Now.AddDays(-7) ' Padrão: 7 dias atrás
        Catch ex As Exception
            Console.WriteLine($"   ⚠️ Erro ao obter última data: {ex.Message}")
            Return DateTime.Now.AddDays(-7) ' Padrão: 7 dias atrás
        End Try
    End Function

    ''' <summary>
    ''' Limpa dados existentes das tabelas
    ''' </summary>
    Private Sub LimparDadosExistentes()
        Try
            ' Limpar tabela incidents
            Dim resultadoIncidents = dbManager.LimparTabela("incidents")
            If resultadoIncidents.Success Then
                Console.WriteLine($"   🗑️ {resultadoIncidents.Mensagem}")
            Else
                Console.WriteLine($"   ⚠️ Erro ao limpar incidents: {resultadoIncidents.Mensagem}")
            End If

            ' Limpar tabela historical_data
            Dim resultadoHistorical = dbManager.LimparTabela("historical_data")
            If resultadoHistorical.Success Then
                Console.WriteLine($"   🗑️ {resultadoHistorical.Mensagem}")
            Else
                Console.WriteLine($"   ⚠️ Erro ao limpar historical_data: {resultadoHistorical.Mensagem}")
            End If

        Catch ex As Exception
            Console.WriteLine($"   ⚠️ Erro ao limpar dados: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Insere dados de teste fixos (método original mantido para compatibilidade)
    ''' </summary>
    Private Sub InserirDadosTeste()
        ' Dados de teste - simulando incidentes reais
        Dim dadosTeste As New List(Of IncidenteTeste) From {
            New IncidenteTeste With {
                .Id = "INC001",
                .Assunto = "Problema de acesso ao sistema",
                .Departamento = "TI",
                .GrupoDirecionado = "Suporte Técnico",
                .Categoria = "Acesso",
                .Prioridade = "Alta",
                .DataCriacao = DateTime.Now.AddDays(-30),
                .DataEncerramento = DateTime.Now.AddDays(-25)
            },
            New IncidenteTeste With {
                .Id = "INC002",
                .Assunto = "Impressora não funciona",
                .Departamento = "Administrativo",
                .GrupoDirecionado = "Suporte Técnico",
                .Categoria = "Hardware",
                .Prioridade = "Média",
                .DataCriacao = DateTime.Now.AddDays(-25),
                .DataEncerramento = DateTime.Now.AddDays(-20)
            },
            New IncidenteTeste With {
                .Id = "INC003",
                .Assunto = "Erro no relatório mensal",
                .Departamento = "Financeiro",
                .GrupoDirecionado = "Desenvolvimento",
                .Categoria = "Software",
                .Prioridade = "Alta",
                .DataCriacao = DateTime.Now.AddDays(-20),
                .DataEncerramento = DateTime.Now.AddDays(-15)
            },
            New IncidenteTeste With {
                .Id = "INC004",
                .Assunto = "Internet lenta",
                .Departamento = "Vendas",
                .GrupoDirecionado = "Infraestrutura",
                .Categoria = "Rede",
                .Prioridade = "Média",
                .DataCriacao = DateTime.Now.AddDays(-15),
                .DataEncerramento = DateTime.Now.AddDays(-10)
            },
            New IncidenteTeste With {
                .Id = "INC005",
                .Assunto = "Backup não realizado",
                .Departamento = "TI",
                .GrupoDirecionado = "Infraestrutura",
                .Categoria = "Backup",
                .Prioridade = "Crítica",
                .DataCriacao = DateTime.Now.AddDays(-10),
                .DataEncerramento = DateTime.Now.AddDays(-5)
            },
            New IncidenteTeste With {
                .Id = "INC006",
                .Assunto = "Usuário não consegue fazer login",
                .Departamento = "RH",
                .GrupoDirecionado = "Suporte Técnico",
                .Categoria = "Acesso",
                .Prioridade = "Alta",
                .DataCriacao = DateTime.Now.AddDays(-5),
                .DataEncerramento = DateTime.Now.AddDays(-2)
            },
            New IncidenteTeste With {
                .Id = "INC007",
                .Assunto = "Sistema de email fora do ar",
                .Departamento = "Marketing",
                .GrupoDirecionado = "Infraestrutura",
                .Categoria = "Email",
                .Prioridade = "Crítica",
                .DataCriacao = DateTime.Now.AddDays(-2),
                .DataEncerramento = DateTime.Now.AddDays(-1)
            },
            New IncidenteTeste With {
                .Id = "INC008",
                .Assunto = "Atualização de software necessária",
                .Departamento = "Operações",
                .GrupoDirecionado = "Desenvolvimento",
                .Categoria = "Software",
                .Prioridade = "Baixa",
                .DataCriacao = DateTime.Now.AddDays(-1),
                .DataEncerramento = Nothing
            }
        }

        Dim registrosInseridos As Integer = 0
        Dim registrosComErro As Integer = 0

        For Each incidente In dadosTeste
            Try
                ' Usar o método do DatabaseManager que recebe os campos individuais
                Dim resultado = dbManager.InserirIncidente(
                    incidente.Id,
                    incidente.Assunto,
                    incidente.Departamento,
                    incidente.GrupoDirecionado,
                    incidente.Categoria,
                    incidente.Prioridade,
                    incidente.DataCriacao,
                    incidente.DataEncerramento)
                If resultado.Success Then
                    registrosInseridos += 1
                Else
                    registrosComErro += 1
                    Console.WriteLine($"   ⚠️ Erro ao inserir incidente {incidente.Id}: {resultado.Mensagem}")
                End If
            Catch ex As Exception
                registrosComErro += 1
                Console.WriteLine($"   ⚠️ Exceção ao inserir incidente {incidente.Id}: {ex.Message}")
            End Try
        Next
        
        Console.WriteLine($"   ✅ Inseridos {registrosInseridos} incidentes de teste")
        If registrosComErro > 0 Then
            Console.WriteLine($"   ⚠️ {registrosComErro} incidentes com erro")
        End If
    End Sub
    
    ''' <summary>
    ''' Atualiza a tabela de dados históricos
    ''' </summary>
    Private Sub AtualizarDadosHistoricos()
        Try
            ' Buscar dados agregados usando DatabaseManager
            Dim resultadoQuery = dbManager.BuscarDadosAgregadosIncidentes()
            
            If Not resultadoQuery.Success Then
                Console.WriteLine($"   ⚠️ Erro ao buscar dados agregados: {resultadoQuery.Mensagem}")
                Return
            End If
            
            If resultadoQuery.Registros Is Nothing OrElse resultadoQuery.Registros.Count = 0 Then
                Console.WriteLine("   ℹ️ Nenhum dado agregado encontrado para inserir na tabela histórica")
                Return
            End If
            
            Dim registrosInseridos As Integer = 0
            Dim registrosComErro As Integer = 0
            
            ' Processar cada registro retornado
            For Each registro In resultadoQuery.Registros
                Try
                    ' Criar objeto DadoHistorico a partir do registro
                    Dim dadoHistorico As New DadoHistorico With {
                        .GroupName = registro("group_name").ToString(),
                        .Data = registro("date").ToString(),
                        .Volume = Convert.ToInt32(registro("volume")),
                        .Category = registro("category").ToString(),
                        .Priority = registro("priority").ToString(),
                        .ResolutionTime = Nothing
                    }
                    
                    ' Usar o método do DatabaseManager que recebe o objeto completo
                    Dim resultado = dbManager.InserirOuAtualizarDadoHistorico(dadoHistorico)
                    If resultado.Success Then
                        registrosInseridos += 1
                    Else
                        registrosComErro += 1
                        Console.WriteLine($"   ⚠️ Erro ao inserir dado histórico: {resultado.Mensagem}")
                    End If
                Catch ex As Exception
                    registrosComErro += 1
                    Console.WriteLine($"   ⚠️ Exceção ao inserir dado histórico: {ex.Message}")
                End Try
            Next
            
            Console.WriteLine($"   ✅ Inseridos {registrosInseridos} registros históricos")
            If registrosComErro > 0 Then
                Console.WriteLine($"   ⚠️ {registrosComErro} registros históricos com erro")
            End If
            
        Catch ex As Exception
            Console.WriteLine($"   ⚠️ Erro ao atualizar dados históricos: {ex.Message}")
        End Try
    End Sub
End Class

' --- Classes de Apoio ---

Public Class IncidenteTeste
    Public Property Id As String
    Public Property Assunto As String
    Public Property Departamento As String
    Public Property GrupoDirecionado As String
    Public Property Categoria As String
    Public Property Prioridade As String
    Public Property DataCriacao As DateTime
    Public Property DataEncerramento As DateTime?
End Class
