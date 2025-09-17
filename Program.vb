Imports System
Imports System.Windows.Forms

Module Program
    ''' <summary>
    ''' Ponto de entrada principal da aplicação WinForms
    ''' </summary>
    <STAThread>
    Sub Main(args As String())
        Try
            ' Configurar aplicação
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            
            ' Verificar argumentos da linha de comando
            If args.Length > 0 Then
                ProcessarArgumentos(args)
            Else
                ' Iniciar aplicação WinForms
                Application.Run(New MainForm())
            End If

        Catch ex As Exception
            MessageBox.Show($"Erro crítico: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ProcessarArgumentos(args As String())
        Select Case args(0).ToLower()
            Case "console"
                ' Modo console para compatibilidade
                ExecutarModoConsole()
            Case "connect"
                ExecutarConexao()
            Case "verify"
                ExecutarVerificacao()
            Case "sync"
                ExecutarSincronizacao()
            Case "query"
                If args.Length > 1 Then
                    Dim queryArgs = New String(args.Length - 2) {}
                    Array.Copy(args, 1, queryArgs, 0, args.Length - 1)
                    Dim queryString = String.Join(" ", queryArgs)
                    ExecutarQuery(queryString)
                Else
                    Console.WriteLine("❌ Query não especificada")
                End If
            Case "help", "-h", "--help"
                MostrarAjuda()
            Case Else
                Console.WriteLine(String.Format("❌ Comando desconhecido: {0}", args(0)))
                MostrarAjuda()
        End Select
    End Sub

    Private Sub ExecutarModoConsole()
        Console.WriteLine("========================================")
        Console.WriteLine("    SUAT Database Manager v1.0")
        Console.WriteLine("    .NET Framework 4.7 - Console Mode")
        Console.WriteLine("========================================")
        Console.WriteLine()

        MostrarMenu()
        
        Dim comando As String
        Do
            Console.Write("Digite um comando (ou 'exit' para sair): ")
            comando = Console.ReadLine()
            
            If Not String.IsNullOrEmpty(comando) Then
                Dim args = comando.Split(" "c)
                ProcessarArgumentos(args)
            End If
        Loop While comando?.ToLower() <> "exit"
    End Sub

    Private Sub MostrarMenu()
        Console.WriteLine("Comandos disponíveis:")
        Console.WriteLine("  connect  - Conectar ao banco SQLite")
        Console.WriteLine("  verify   - Verificar estrutura do banco")
        Console.WriteLine("  sync     - Sincronizar dados (teste)")
        Console.WriteLine("  query    - Executar query SQL")
        Console.WriteLine("  help     - Mostrar esta ajuda")
        Console.WriteLine("  exit     - Sair do modo console")
        Console.WriteLine()
        Console.WriteLine("Exemplo: SuatDatabaseManager.exe console")
        Console.WriteLine()
    End Sub

    Private Sub MostrarAjuda()
        Console.WriteLine("Uso: SuatDatabaseManager.exe [comando] [opções]")
        Console.WriteLine()
        Console.WriteLine("Comandos:")
        Console.WriteLine("  (sem argumentos)           Iniciar interface gráfica")
        Console.WriteLine("  console                    Iniciar modo console interativo")
        Console.WriteLine("  connect                    Conectar ao banco SQLite")
        Console.WriteLine("  verify                     Verificar estrutura do banco")
        Console.WriteLine("  sync                       Sincronizar dados (teste)")
        Console.WriteLine("  query <sql>                Executar query SQL")
        Console.WriteLine("  help                       Mostrar esta ajuda")
        Console.WriteLine()
        Console.WriteLine("Exemplos:")
        Console.WriteLine("  SuatDatabaseManager.exe                    # Interface gráfica")
        Console.WriteLine("  SuatDatabaseManager.exe console            # Modo console")
        Console.WriteLine("  SuatDatabaseManager.exe connect")
        Console.WriteLine("  SuatDatabaseManager.exe verify")
        Console.WriteLine("  SuatDatabaseManager.exe sync")
        Console.WriteLine("  SuatDatabaseManager.exe query ""SELECT COUNT(*) FROM incidents""")
    End Sub

    Private Sub ExecutarConexao()
        Console.WriteLine("🔌 Conectando ao banco SQLite...")

        Try
            Dim manager As New DatabaseManager()
            Dim resultado = manager.Conectar()

            If resultado.Success Then
                Console.WriteLine("✅ Conexão estabelecida com sucesso!")
                Console.WriteLine(String.Format("📊 Total de tabelas: {0}", resultado.TotalTabelas))
                Console.WriteLine(String.Format("📁 Tamanho do arquivo: {0:N0} bytes", resultado.TamanhoArquivo))
            Else
                Console.WriteLine(String.Format("❌ Erro na conexão: {0}", resultado.Mensagem))
            End If

        Catch ex As Exception
            Console.WriteLine(String.Format("❌ Erro: {0}", ex.Message))
        End Try
    End Sub

    Private Sub ExecutarVerificacao()
        Console.WriteLine("🔍 Verificando estrutura do banco...")

        Try
            Dim manager As New DatabaseManager()
            Dim resultado = manager.VerificarEstrutura()

            If resultado.Success Then
                Console.WriteLine("✅ Verificação concluída!")
                Console.WriteLine(String.Format("📋 Tabelas encontradas: {0}", resultado.Tabelas.Count))

                For Each tabela In resultado.Tabelas
                    Console.WriteLine(String.Format("  - {0}: {1:N0} registros", tabela.Nome, tabela.Registros))
                Next
            Else
                Console.WriteLine(String.Format("❌ Erro na verificação: {0}", resultado.Mensagem))
            End If

        Catch ex As Exception
            Console.WriteLine(String.Format("❌ Erro: {0}", ex.Message))
        End Try
    End Sub

    Private Sub ExecutarSincronizacao()
        Console.WriteLine("🔄 Iniciando sincronização de dados (teste)...")

        Try
            Dim sincronizador As New SincronizadorDados()
            sincronizador.ExecutarSincronizacaoTeste()

            Console.WriteLine("✅ Sincronização concluída com sucesso!")

        Catch ex As Exception
            Console.WriteLine(String.Format("❌ Erro na sincronização: {0}", ex.Message))
        End Try
    End Sub

    Private Sub ExecutarQuery(query As String)
        Console.WriteLine(String.Format("🔍 Executando query: {0}", query))

        Try
            Dim manager As New DatabaseManager()
            Dim resultado = manager.ExecutarQuery(query)

            If resultado.Success Then
                Console.WriteLine(String.Format("✅ Query executada. {0} linhas retornadas", resultado.Registros.Count))

                If resultado.Registros.Count > 0 Then
                    ' Mostrar cabeçalhos
                    Dim cabecalhos = resultado.Registros(0).Keys.ToArray()
                    Dim cabecalhosStr = String.Join(" | ", cabecalhos)
                    Console.WriteLine(cabecalhosStr)
                    Console.WriteLine(New String("-"c, cabecalhos.Length * 20))

                    ' Mostrar primeiras 10 linhas
                    For i As Integer = 0 To Math.Min(9, resultado.Registros.Count - 1)
                        Dim linha = resultado.Registros(i)
                        Dim valores = cabecalhos.Select(Function(c) If(linha(c) IsNot Nothing, linha(c).ToString(), "NULL")).ToArray()
                        Dim valoresStr = String.Join(" | ", valores)
                        Console.WriteLine(valoresStr)
                    Next

                    If resultado.Registros.Count > 10 Then
                        Console.WriteLine(String.Format("... e mais {0} linhas", resultado.Registros.Count - 10))
                    End If
                End If
            Else
                Console.WriteLine(String.Format("❌ Erro na query: {0}", resultado.Mensagem))
            End If

        Catch ex As Exception
            Console.WriteLine(String.Format("❌ Erro: {0}", ex.Message))
        End Try
    End Sub
End Module
