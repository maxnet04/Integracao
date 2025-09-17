Imports System.Data.SQLite
Imports System.IO
Imports System.Collections.Generic
Imports System.Linq

''' <summary>
''' Classe responsável por gerenciar conexões e operações no banco SQLite
''' </summary>
Public Class DatabaseManager
    Private connection As SQLiteConnection
    Private databasePath As String
    
    Public Sub New(Optional dbPath As String = Nothing)
        If String.IsNullOrEmpty(dbPath) Then
            ' Caminho relativo ao banco de dados (voltando um nível para a pasta data)
            databasePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "database.sqlite")

            ' Verificar se o arquivo existe
            If Not File.Exists(databasePath) Then
                Throw New FileNotFoundException(String.Format("Banco de dados SQLite não encontrado em: {0}", databasePath))
            End If
        Else
            databasePath = dbPath
        End If
    End Sub
    
    ''' <summary>
    ''' Conecta ao banco de dados SQLite
    ''' </summary>
    Public Function Conectar() As ConexaoResult
        Try
            If Not File.Exists(databasePath) Then
                Return New ConexaoResult With {
                    .Success = False,
                    .Mensagem = $"Arquivo de banco não encontrado: {databasePath}"
                }
            End If
            
            Dim connectionString As String = $"Data Source={databasePath};Version=3;"
            connection = New SQLiteConnection(connectionString)
            connection.Open()

            ' Verificar e criar estrutura das tabelas se necessário
            VerificarECriarEstruturaTabelas()

            ' Verificar informações do banco
            Dim totalTabelas As Integer = 0
            Using cmd As New SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table'", connection)
                totalTabelas = Convert.ToInt32(cmd.ExecuteScalar())
            End Using

            Dim fileInfo As New System.IO.FileInfo(databasePath)

            Return New ConexaoResult With {
                .Success = True,
                .Mensagem = "Conexão estabelecida com sucesso",
                .TotalTabelas = totalTabelas,
                .TamanhoArquivo = fileInfo.Length
            }

        Catch ex As Exception
            Return New ConexaoResult With {
                .Success = False,
                .Mensagem = $"Erro ao conectar: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Verifica a estrutura do banco de dados
    ''' </summary>
    Public Function VerificarEstrutura() As VerificacaoResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New VerificacaoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Dim tabelas As New List(Of TabelaInfo)()

            ' Buscar todas as tabelas
            Using cmd As New SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", connection)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim nomeTabela As String = reader.GetString(0)

                        ' Contar registros
                        Dim totalRegistros As Long = 0
                        Try
                            Using countCmd As New SQLiteCommand($"SELECT COUNT(*) FROM [{nomeTabela}]", connection)
                                totalRegistros = Convert.ToInt64(countCmd.ExecuteScalar())
                            End Using
                        Catch
                            ' Ignorar erros ao contar registros
                        End Try

                        tabelas.Add(New TabelaInfo With {
                            .Nome = nomeTabela,
                            .Registros = totalRegistros
                        })
                    End While
                End Using
            End Using

            Return New VerificacaoResult With {
                .Success = True,
                .Mensagem = "Verificação concluída",
                .Tabelas = tabelas
            }

        Catch ex As Exception
            Return New VerificacaoResult With {
                .Success = False,
                .Mensagem = $"Erro na verificação: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Executa uma query SQL
    ''' </summary>
    Public Function ExecutarQuery(query As String) As QueryResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New QueryResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Dim registros As New List(Of Dictionary(Of String, Object))()

            Using cmd As New SQLiteCommand(query, connection)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim registro As New Dictionary(Of String, Object)()

                        For i As Integer = 0 To reader.FieldCount - 1
                            Dim nomeColuna As String = reader.GetName(i)
                            Dim valor As Object = If(reader.IsDBNull(i), Nothing, reader.GetValue(i))
                            registro.Add(nomeColuna, valor)
                        Next

                        registros.Add(registro)
                    End While
                End Using
            End Using

            Return New QueryResult With {
                .Success = True,
                .Mensagem = "Query executada com sucesso",
                .Registros = registros
            }

        Catch ex As Exception
            Return New QueryResult With {
                .Success = False,
                .Mensagem = $"Erro na query: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Executa um comando INSERT na tabela especificada
    ''' </summary>
    Public Function ExecutarInsert(tabela As String, dados As Dictionary(Of String, Object)) As ComandoResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            If dados Is Nothing OrElse dados.Count = 0 Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Dados não fornecidos para inserção"
                }
            End If

            ' Construir query INSERT
            Dim colunas As String = String.Join(", ", dados.Keys.Select(Function(k) $"[{k}]"))
            Dim parametros As String = String.Join(", ", dados.Keys.Select(Function(k) $"@{k}"))
            Dim query As String = $"INSERT INTO [{tabela}] ({colunas}) VALUES ({parametros})"

            Dim linhasAfetadas As Integer = 0

            Using cmd As New SQLiteCommand(query, connection)
                ' Adicionar parâmetros
                For Each kvp In dados
                    cmd.Parameters.AddWithValue($"@{kvp.Key}", If(kvp.Value, DBNull.Value))
                Next

                linhasAfetadas = cmd.ExecuteNonQuery()
            End Using

            Return New ComandoResult With {
                .Success = True,
                .Mensagem = $"Inserção realizada com sucesso. {linhasAfetadas} linha(s) afetada(s)",
                .LinhasAfetadas = linhasAfetadas
            }

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro na inserção: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Executa um comando UPDATE na tabela especificada
    ''' </summary>
    Public Function ExecutarUpdate(tabela As String, dados As Dictionary(Of String, Object), condicao As String, parametrosCondicao As Dictionary(Of String, Object)) As ComandoResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            If dados Is Nothing OrElse dados.Count = 0 Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Dados não fornecidos para atualização"
                }
            End If

            ' Construir query UPDATE
            Dim setClause As String = String.Join(", ", dados.Keys.Select(Function(k) $"[{k}] = @set_{k}"))
            Dim query As String = $"UPDATE [{tabela}] SET {setClause}"

            If Not String.IsNullOrEmpty(condicao) Then
                query &= $" WHERE {condicao}"
            End If

            Dim linhasAfetadas As Integer = 0

            Using cmd As New SQLiteCommand(query, connection)
                ' Adicionar parâmetros de SET
                For Each kvp In dados
                    cmd.Parameters.AddWithValue($"@set_{kvp.Key}", If(kvp.Value, DBNull.Value))
                Next

                ' Adicionar parâmetros da condição WHERE
                If parametrosCondicao IsNot Nothing Then
                    For Each kvp In parametrosCondicao
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", If(kvp.Value, DBNull.Value))
                    Next
                End If

                linhasAfetadas = cmd.ExecuteNonQuery()
            End Using

            Return New ComandoResult With {
                .Success = True,
                .Mensagem = $"Atualização realizada com sucesso. {linhasAfetadas} linha(s) afetada(s)",
                .LinhasAfetadas = linhasAfetadas
            }

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro na atualização: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Executa um comando DELETE na tabela especificada
    ''' </summary>
    Public Function ExecutarDelete(tabela As String, condicao As String, parametrosCondicao As Dictionary(Of String, Object)) As ComandoResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            ' Construir query DELETE
            Dim query As String = $"DELETE FROM [{tabela}]"

            If Not String.IsNullOrEmpty(condicao) Then
                query &= $" WHERE {condicao}"
            End If

            Dim linhasAfetadas As Integer = 0

            Using cmd As New SQLiteCommand(query, connection)
                ' Adicionar parâmetros da condição WHERE
                If parametrosCondicao IsNot Nothing Then
                    For Each kvp In parametrosCondicao
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", If(kvp.Value, DBNull.Value))
                    Next
                End If

                linhasAfetadas = cmd.ExecuteNonQuery()
            End Using

            Return New ComandoResult With {
                .Success = True,
                .Mensagem = $"Exclusão realizada com sucesso. {linhasAfetadas} linha(s) afetada(s)",
                .LinhasAfetadas = linhasAfetadas
            }

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro na exclusão: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Executa um comando SQL que não retorna dados (INSERT, UPDATE, DELETE)
    ''' </summary>
    Public Function ExecutarComando(query As String, parametros As Dictionary(Of String, Object)) As ComandoResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Dim linhasAfetadas As Integer = 0

            Using cmd As New SQLiteCommand(query, connection)
                ' Adicionar parâmetros se fornecidos
                If parametros IsNot Nothing Then
                    For Each kvp In parametros
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", If(kvp.Value, DBNull.Value))
                    Next
                End If

                linhasAfetadas = cmd.ExecuteNonQuery()
            End Using

            Return New ComandoResult With {
                .Success = True,
                .Mensagem = $"Comando executado com sucesso. {linhasAfetadas} linha(s) afetada(s)",
                .LinhasAfetadas = linhasAfetadas
            }

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro no comando: {ex.Message}"
            }
        End Try
    End Function

    ' ===== MÉTODOS ESPECIALIZADOS PARA SINCRONIZAÇÃO =====

    ''' <summary>
    ''' Limpa todos os registros de uma tabela específica
    ''' </summary>
    Public Function LimparTabela(nomeTabela As String) As ComandoResult
        Try
            If String.IsNullOrWhiteSpace(nomeTabela) Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Nome da tabela não pode ser vazio"
                }
            End If

            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Dim query As String = $"DELETE FROM [{nomeTabela}]"
            Return ExecutarComando(query, Nothing)

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro ao limpar tabela '{nomeTabela}': {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Insere um incidente na tabela incidents usando campos individuais
    ''' </summary>
    Public Function InserirIncidente(
        id As String,
        assunto As String,
        departamento As String,
        grupoDirecionado As String,
        categoria As String,
        prioridade As String,
        dataCriacao As DateTime,
        dataEncerramento As DateTime?) As ComandoResult
        Try
            ' Validações de entrada
            If String.IsNullOrWhiteSpace(grupoDirecionado) Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Grupo direcionado não pode ser vazio"
                }
            End If

            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Const insertSql As String = "
                INSERT INTO incidents (
                    incident_date, created_at, closed_at, category, assigned_group, priority, status, volume
                ) VALUES (
                    @incident_date, @created_at, @closed_at, @category, @assigned_group, @priority, @status, @volume
                )"

            Dim parametros As New Dictionary(Of String, Object)()
            parametros.Add("incident_date", dataCriacao.ToString("yyyy-MM-dd"))
            parametros.Add("created_at", dataCriacao.ToString("o"))

            ' Tratar data de encerramento
            If dataEncerramento.HasValue Then
                parametros.Add("closed_at", dataEncerramento.Value.ToString("o"))
            Else
                parametros.Add("closed_at", DBNull.Value)
            End If

            parametros.Add("category", If(String.IsNullOrEmpty(categoria), "Não especificado", categoria))
            parametros.Add("assigned_group", grupoDirecionado)
            parametros.Add("priority", If(String.IsNullOrEmpty(prioridade), "Média", prioridade))
            parametros.Add("status", If(dataEncerramento.HasValue, "RESOLVIDO", "DIRECIONADO"))
            parametros.Add("volume", 1)

            Return ExecutarComando(insertSql, parametros)

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro ao inserir incidente: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Insere ou atualiza um registro na tabela historical_data usando dados agregados
    ''' </summary>
    Public Function InserirOuAtualizarDadoHistorico(dadoHistorico As DadoHistorico) As ComandoResult
        Try
            ' Validações de entrada
            If dadoHistorico Is Nothing Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Dado histórico não pode ser nulo"
                }
            End If

            If String.IsNullOrWhiteSpace(dadoHistorico.GroupName) Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Group name não pode ser vazio"
                }
            End If

            If String.IsNullOrWhiteSpace(dadoHistorico.Data) Then
                Return New ComandoResult With {
                    .Success = False,
                    .Mensagem = "Data não pode ser vazia"
                }
            End If

            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New ComandoResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Const insertSql As String = "
                INSERT OR REPLACE INTO historical_data (
                    assigned_group, date, volume, category, priority,
                    resolution_time, created_at, updated_at
                ) VALUES (
                    @assigned_group, @date, @volume, @category, @priority,
                    @resolution_time, datetime('now', 'localtime'), datetime('now', 'localtime')
                )"

            Dim parametros As New Dictionary(Of String, Object)()
            parametros.Add("assigned_group", dadoHistorico.GroupName)
            parametros.Add("date", dadoHistorico.Data)
            parametros.Add("volume", Math.Max(0, dadoHistorico.Volume))
            parametros.Add("category", If(String.IsNullOrEmpty(dadoHistorico.Category), "Não especificado", dadoHistorico.Category))
            parametros.Add("priority", If(String.IsNullOrEmpty(dadoHistorico.Priority), "Média", dadoHistorico.Priority))
            parametros.Add("resolution_time", If(dadoHistorico.ResolutionTime Is Nothing, DBNull.Value, dadoHistorico.ResolutionTime))

            Return ExecutarComando(insertSql, parametros)

        Catch ex As Exception
            Return New ComandoResult With {
                .Success = False,
                .Mensagem = $"Erro ao inserir/atualizar dado histórico: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Busca dados agregados de incidentes para inserção na tabela historical_data
    ''' </summary>
    Public Function BuscarDadosAgregadosIncidentes() As QueryResult
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Dim conexaoResult = Conectar()
                If Not conexaoResult.Success Then
                    Return New QueryResult With {
                        .Success = False,
                        .Mensagem = conexaoResult.Mensagem
                    }
                End If
            End If

            Const selectQuery As String = "
                SELECT 
                    assigned_group as group_name,
                    incident_date as date,
                    CAST(COUNT(*) AS INTEGER) as volume,
                    category,
                    priority
                FROM incidents 
                WHERE assigned_group IS NOT NULL 
                  AND incident_date IS NOT NULL
                  AND assigned_group != ''
                  AND incident_date != ''
                GROUP BY assigned_group, incident_date
                ORDER BY incident_date DESC, assigned_group"

            Return ExecutarQuery(selectQuery)

        Catch ex As Exception
            Return New QueryResult With {
                .Success = False,
                .Mensagem = $"Erro ao buscar dados agregados: {ex.Message}"
            }
        End Try
    End Function

    ''' <summary>
    ''' Verifica e cria a estrutura das tabelas se necessário
    ''' </summary>
    Private Sub VerificarECriarEstruturaTabelas()
        Try
            ' Criar tabela incidents se não existir
            Const createIncidentsTable As String = "
                CREATE TABLE IF NOT EXISTS incidents (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    incident_date TEXT NOT NULL,
                    created_at DATETIME NOT NULL,
                    closed_at DATETIME,
                    category TEXT,
                    assigned_group TEXT,
                    priority TEXT,
                    status TEXT CHECK(status IN ('CANCELADO', 'RESOLVIDO', 'DIRECIONADO')),
                    volume INTEGER NOT NULL DEFAULT 1,
                    is_anomaly INTEGER DEFAULT 0,
                    anomaly_type TEXT
                )"

            Using cmd As New SQLiteCommand(createIncidentsTable, connection)
                cmd.ExecuteNonQuery()
            End Using

            ' Criar tabela historical_data se não existir
            Const createHistoricalDataTable As String = "
                CREATE TABLE IF NOT EXISTS historical_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    assigned_group TEXT NOT NULL,
                    date TEXT NOT NULL,
                    volume INTEGER NOT NULL,
                    category TEXT,
                    priority TEXT,
                    resolution_time INTEGER,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(assigned_group, date)
                )"

            Using cmd As New SQLiteCommand(createHistoricalDataTable, connection)
                cmd.ExecuteNonQuery()
            End Using

            ' Criar tabela users se não existir
            Const createUsersTable As String = "
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    password TEXT NOT NULL,
                    role TEXT NOT NULL
                )"

            Using cmd As New SQLiteCommand(createUsersTable, connection)
                cmd.ExecuteNonQuery()
            End Using

            ' Criar tabela notifications se não existir
            Const createNotificationsTable As String = "
                CREATE TABLE IF NOT EXISTS notifications (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    group_id TEXT NOT NULL,
                    message TEXT NOT NULL,
                    type TEXT NOT NULL,
                    severity TEXT NOT NULL,
                    related_entity TEXT,
                    related_id TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    read_at DATETIME,
                    UNIQUE(group_id, message, created_at)
                )"

            Using cmd As New SQLiteCommand(createNotificationsTable, connection)
                cmd.ExecuteNonQuery()
            End Using

        Catch ex As Exception
            ' Log do erro mas não falhar a conexão
            Console.WriteLine($"Aviso: Erro ao verificar estrutura das tabelas: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Verifica se o banco de dados existe
    ''' </summary>
    Public Function VerificarExistenciaBanco() As Boolean
        Try
            Return File.Exists(databasePath)
        Catch ex As Exception
            Return False
        End Try
    End Function
    
    ''' <summary>
    ''' Fecha a conexão com o banco
    ''' </summary>
    Public Sub Desconectar()
        Try
            If connection IsNot Nothing AndAlso connection.State = ConnectionState.Open Then
                connection.Close()
                connection.Dispose()
                connection = Nothing
            End If
        Catch ex As Exception
            ' Ignorar erros ao desconectar
        End Try
    End Sub
    
    ''' <summary>
    ''' Destrutor da classe
    ''' </summary>
    Protected Overrides Sub Finalize()
        Desconectar()
        MyBase.Finalize()
    End Sub
End Class

' --- Classes de Resultado ---

Public Class ConexaoResult
    Public Property Success As Boolean
    Public Property Mensagem As String
    Public Property TotalTabelas As Integer
    Public Property TamanhoArquivo As Long
End Class

Public Class VerificacaoResult
    Public Property Success As Boolean
    Public Property Mensagem As String
    Public Property Tabelas As List(Of TabelaInfo)
End Class

Public Class QueryResult
    Public Property Success As Boolean
    Public Property Mensagem As String
    Public Property Registros As List(Of Dictionary(Of String, Object))
End Class

Public Class TabelaInfo
    Public Property Nome As String
    Public Property Registros As Long
End Class

Public Class ComandoResult
    Public Property Success As Boolean
    Public Property Mensagem As String
    Public Property LinhasAfetadas As Integer
End Class

Public Class DadoHistorico
    Public Property GroupName As String
    Public Property Data As String
    Public Property Volume As Integer
    Public Property Category As String
    Public Property Priority As String
    Public Property ResolutionTime As Object
End Class

