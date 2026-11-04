Public Class PageLoginMs

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean)
        Dim IndexBefore = ComboAccounts.SelectedIndex
        '刷新下拉框列表
        ComboAccounts.Items.Clear()
        ComboAccounts.Items.Add(New MyComboBoxItem With {.Content = "添加新账号"})
        Try
            Dim MsJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
            For Each Account In MsJson
                Dim Item As MyListItem = CType(FindResource("ComboBoxItemTemplateWithDelete"), DataTemplate).LoadContent()
                Item.Tag = Account.Value.ToString
                Item.Title = Account.Key
                CType(Item.Buttons(0), MyIconButton).Tag = Account.Key
                ComboAccounts.Items.Add(Item)
            Next
        Catch ex As Exception
            Logger.Error(ex, $"微软登录信息出错，登录信息已被重置", LogBehavior.Toast)
            Settings.Set("LoginMsJson", "{}")
        End Try
        '如果不保留输入，刷新列表后自动选择第一项
        ComboAccounts.SelectedIndex = If(KeepInput, Math.Max(0, IndexBefore), 0)
        '回填已保存的 Client ID，空字符串表示当前用的是内置默认值
        Try
            TextClientId.Text = If(Settings.Get(Of String)("MsOAuthClientId"), "")
        Catch
            TextClientId.Text = ""
        End Try
    End Sub

    ''' <summary>
    ''' 保存用户填写的微软应用 Client ID。留空则恢复内置默认值。
    ''' </summary>
    Private Sub BtnSaveClientId_Click(sender As Object, e As EventArgs) Handles BtnSaveClientId.Click
        Dim NewId As String = If(TextClientId.Text, "").Trim()
        If NewId.Length > 0 AndAlso NewId.Length < 20 Then
            Hint("Client ID 看起来不完整，应该是一串较长的字符，请检查是否复制全了", HintType.Red)
            Return
        End If
        OAuthClientId = NewId
        TextClientId.Text = If(Settings.Get(Of String)("MsOAuthClientId"), "")
        If NewId.Length = 0 Then
            Hint("已清空 Client ID，登录前需要重新填写")
        Else
            Hint("Client ID 已保存，之后的正版登录都会使用它")
        End If
    End Sub

    ''' <summary>
    ''' 「怎么获取 Client ID」的说明弹窗。
    ''' </summary>
    Private Sub BtnClientIdHelp_Click(sender As Object, e As EventArgs) Handles BtnClientIdHelp.Click
        MyMsgBox(
            "Client ID 是微软用来分辨「哪个程序在登录」的一个编号，自己申请一个就能用，不要钱，也不用审核。" & vbCrLf & vbCrLf &
            "1. 点下面的「去注册」，用你自己的微软账号登录（个人账号就行）" & vbCrLf &
            "2. 点左上角「新注册」" & vbCrLf &
            "3. 名称随便写，比如 PCL；「支持的账户类型」选第二个，只选个人账号的那项" & vbCrLf &
            "4. 点「注册」，进到应用页后复制「应用程序(客户端) ID」那一串" & vbCrLf &
            "5. 回来粘到上面的输入框，点确定" & vbCrLf & vbCrLf &
            "不用配重定向地址，也不用申请接口权限，注册完直接能用。",
            "怎么获取 Client ID",
            "去注册", "关掉",
            Button1Action:=Sub() OpenWebsite("https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/CreateApplicationBlade/quickStartType~/null/isMSAApp~/false"))
    End Sub
    Private Sub ComboAccounts_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAccounts.SelectionChanged
        If AniControlEnabled <> 0 OrElse ComboAccounts.SelectedItem Is Nothing OrElse ComboAccounts.ContentPresenter Is Nothing Then Return
        If TypeOf ComboAccounts.SelectedItem Is MyListItem Then
            ComboAccounts.ContentPresenter.Content = CType(ComboAccounts.SelectedItem, MyListItem).Title
        ElseIf TypeOf ComboAccounts.SelectedItem Is MyComboBoxItem Then
            ComboAccounts.ContentPresenter.Content = CType(ComboAccounts.SelectedItem, MyComboBoxItem).Content
        End If
    End Sub

    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginMs
        If FrmLoginMs Is Nothing Then Return New McLoginMs With {.OAuthRefreshToken = Settings.Get(Of String)("CacheMsV2OAuthRefresh"), .UserName = Settings.Get(Of String)("CacheMsV2Name")}
        Dim Result As McLoginMs = Nothing
        RunInUiWait(
        Sub()
            If FrmLoginMs.ComboAccounts.SelectedIndex = 0 Then
                Result = New McLoginMs
            Else
                Dim Item As MyListItem = FrmLoginMs.ComboAccounts.SelectedItem
                Result = New McLoginMs With {.OAuthRefreshToken = Item.Tag, .UserName = Item.Title}
            End If
        End Sub)
        Return Result
    End Function
    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginMs) As String
        If LoginData.OAuthRefreshToken = "" Then
            Return "请在登录账号后再启动游戏！"
        Else
            Return ""
        End If
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        '没有 Client ID 就没法发起设备码请求，先拦下来，别让用户对着转圈发呆
        If String.IsNullOrWhiteSpace(OAuthClientId) Then
            Hint("先填一下 Client ID 再登录，点下面的「怎么获取 Client ID」有教程", HintType.Red)
            Return
        End If
        ComboAccounts.IsEnabled = False
        BtnLogin.IsEnabled = False
        BtnLogin.Text = "0%"
        RunInNewThread(
        Sub()
            Try
                McLoginMsLoader.Start(GetLoginData(), IsForceRestart:=True)
                Do While McLoginMsLoader.State = LoadState.Loading
                    RunInUi(Sub() BtnLogin.Text = Math.Round(McLoginMsLoader.Progress * 100) & "%")
                    Thread.Sleep(50)
                Loop
                If McLoginMsLoader.State = LoadState.Finished Then
                    RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
                ElseIf McLoginMsLoader.State = LoadState.Canceled Then
                    Throw New OperationCanceledException
                ElseIf McLoginMsLoader.Error Is Nothing Then
                    Throw New Exception("未知错误！")
                Else
                    Throw New Exception(McLoginMsLoader.Error.Message, McLoginMsLoader.Error)
                End If
            Catch ex As Exception
                If ex.IsCanceled Then
                ElseIf ex.Message.StartsWithF("$") Then
                    Hint(ex.Message.TrimStart("$"), HintType.Red)
                ElseIf TypeOf ex Is Security.Authentication.AuthenticationException AndAlso ex.Message.Contains("SSL/TLS") Then
                    Logger.Error(ex, $"正版登录验证失败，请考虑在 [设置 → 其他] 中关闭 [在正版登录时验证 SSL 证书]，然后再试。{vbCrLf}{vbCrLf}原始错误信息：", LogBehavior.Alert)
                Else
                    Logger.Error(ex, "正版登录尝试失败", LogBehavior.Alert)
                End If
            Finally
                RunInUi(
                Sub()
                    ComboAccounts.IsEnabled = True
                    BtnLogin.IsEnabled = True
                    BtnLogin.Text = "登录"
                End Sub)
            End Try
        End Sub, "Ms Login")
    End Sub

End Class
