Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>
''' 百宝箱页面。界面与功能照 PCL 官方版复刻：
''' 今日人品、内存优化、清理游戏垃圾、千万别点、回声洞、下载自定义文件。
''' </summary>
Public Class PageOtherTest

#Region "界面事件"

    Private Sub BtnJrrp_Click(sender As Object, e As EventArgs) Handles BtnJrrp.Click
        Jrrp()
    End Sub

    Private Sub BtnMemory_Click(sender As Object, e As EventArgs) Handles BtnMemory.Click
        MemoryOptimize(True)
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs) Handles BtnClear.Click
        RubbishClear()
    End Sub

    Private Sub BtnClick_Click(sender As Object, e As EventArgs) Handles BtnClick.Click
        DontClick()
    End Sub

    Private Sub CardCave_Click(sender As Object, e As MouseButtonEventArgs) Handles CardCave.MouseLeftButtonUp
        LabCave.Text = GetRandomCave()
    End Sub

    Private Sub BtnDownloadStart_Click(sender As Object, e As EventArgs) Handles BtnDownloadStart.Click
        StartCustomDownload(TextDownloadUrl.Text, TextDownloadName.Text, TextDownloadFolder.Text)
        TextDownloadUrl.Text = ""
        TextDownloadName.Text = ""
    End Sub

    Private Sub BtnDownloadSelect_Click(sender As Object, e As EventArgs) Handles BtnDownloadSelect.Click
        Dim Target As String = Dialogs.SelectFolder("选择文件保存位置", False).FirstOrDefault
        If Target IsNot Nothing Then TextDownloadFolder.Text = Target
    End Sub

    Private Sub BtnDownloadOpen_Click(sender As Object, e As EventArgs) Handles BtnDownloadOpen.Click
        Dim Folder As String = TextDownloadFolder.Text
        If String.IsNullOrWhiteSpace(Folder) OrElse Not Directory.Exists(Folder) Then
            Hint("这个文件夹还不存在，先选一个吧", HintType.Red)
            Return
        End If
        OpenExplorer(Folder)
    End Sub

    Private Sub PageLoaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If String.IsNullOrWhiteSpace(TextDownloadFolder.Text) Then
            TextDownloadFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        End If
    End Sub

#End Region

#Region "今日人品"

    ''' <summary>
    ''' 今日人品：按当天日期算一个稳定的分数，同一天点多少次都是同一个值。
    ''' </summary>
    Public Shared Sub Jrrp()
        Dim Seed As Integer = Math.Abs($"asdfgbn{Date.Now.DayOfYear}12#3$45{Date.Now.Year}IUY".GetStableHashCode())
        Dim Num As Integer = Seed Mod 1000
        Dim Score As Integer = If(Num < 970, CInt(Math.Round(Num / 969 * 99)), 100)

        Dim Tail As String
        Select Case True
            Case Score = 100 : Tail = "！100！100！！！！！"
            Case Score >= 98 : Tail = "！差点就到 100 了呢……"
            Case Score >= 90 : Tail = "！好评如潮！"
            Case Score >= 65 : Tail = "！今天运气不错呢！"
            Case Score > 50 : Tail = "！还行啦，还行啦。"
            Case Score = 50 : Tail = "！五五开……"
            Case Score >= 40 : Tail = "！勉强还行吧……？"
            Case Score >= 20 : Tail = "！呜……"
            Case Score >= 11 : Tail = "？不会吧……"
            Case Score >= 1 : Tail = "……（是百分制哦）"
            Case Else : Tail = "？！"
        End Select

        If MyMsgBox("在查看结果前，请先同意以下附加使用条款：" & vbCrLf & vbCrLf &
                    "1. 我知晓并了解 PCL 的今日人品功能完全没有出 Bug。" & vbCrLf &
                    "2. PCL 不对使用本软件所间接造成的一切财产损失（如砸电脑等）等负责。",
                    "今日人品 - 附加使用条款", "同意并查看结果", "再见", IsWarn:=True) = 2 Then Return
        MyMsgBox("你今天的人品值是：" & Score & Tail, "今日人品 - " & Score)
    End Sub

#End Region

#Region "内存优化"

    <DllImport("psapi.dll", SetLastError:=True)>
    Private Shared Function EmptyWorkingSet(hProcess As IntPtr) As Boolean
    End Function

    ''' <summary>
    ''' 内存优化：把当前进程的工作集还给系统。会短暂卡一下，属正常。
    ''' </summary>
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If ShowHint Then Hint("正在进行内存优化……")
        MemoryOptimizeInternal(ShowHint)
        If ShowHint Then Hint("内存优化完成！")
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        Try
            Dim Proc As Process = Process.GetCurrentProcess()
            If Not EmptyWorkingSet(Proc.Handle) Then
                Logger.Info($"内存优化 API 返回失败，错误码 {Marshal.GetLastWin32Error()}")
            End If
            Proc.Dispose()
        Catch ex As Exception
            Logger.Error(ex, "内存优化失败")
            If ShowHint Then Hint("内存优化失败了，详细信息见日志", HintType.Red)
        End Try
    End Sub

#End Region

#Region "清理垃圾"

    ''' <summary>
    ''' 清理游戏垃圾：各版本目录下的日志、崩溃报告、调试输出，以及 PCL 临时目录。
    ''' </summary>
    Public Shared Sub RubbishClear()
        Try
            Dim Removed As Integer = 0
            Dim Wiped As Integer = 0

            Dim ClearDir As Action(Of String) =
                Sub(Dir As String)
                    Try
                        If Not Directory.Exists(Dir) Then Return
                        Directory.Delete(Dir, True)
                        Wiped += 1
                    Catch ex As Exception
                        Logger.Warn(ex, $"清理文件夹失败（{Dir}）")
                    End Try
                End Sub

            Dim McFolder As String = Path.Combine(Paths.Base, ".minecraft")
            If Directory.Exists(McFolder) Then
                For Each Inst As String In Directory.GetDirectories(McFolder)
                    ClearDir(Path.Combine(Inst, ".mixin.out"))
                    ClearDir(Path.Combine(Inst, "crash-reports"))
                    ClearDir(Path.Combine(Inst, "debug"))
                    ClearDir(Path.Combine(Inst, "logs"))
                    Try
                        For Each FilePath As String In Directory.EnumerateFiles(Inst, "*", SearchOption.TopDirectoryOnly)
                            Dim Name As String = Path.GetFileName(FilePath)
                            If Name.StartsWithF("hs_err_pid") OrElse Path.GetExtension(Name).ToLowerInvariant() = ".log" Then
                                File.Delete(FilePath)
                                Removed += 1
                            End If
                        Next
                    Catch ex As Exception
                        Logger.Warn(ex, $"清理文件失败（{Inst}）")
                    End Try
                Next
            End If

            ClearDir(PathTemp)
            ClearDir(Path.Combine(OsDrive, "ProgramData\PCL\"))

            Dim Summary As String = $"清理完毕：删掉了 {Wiped} 个日志目录和 {Removed} 个散落的日志文件。"
            If MyMsgBox(Summary, "清理完成", "关闭 PCL", "重启 PCL", IsWarn:=False, ForceWait:=True) = 2 Then
                Process.Start(New ProcessStartInfo(PathExe, "--wait"))
            End If
            FormMain.EndProgramForce()
        Catch ex As Exception
            Logger.Error(ex, "清理垃圾失败")
            Hint("清理垃圾失败了，详细信息见日志", HintType.Red)
        End Try
    End Sub

#End Region

#Region "千万别点"

    Private Shared DontClickStage As Integer = 0

    ''' <summary>
    ''' 彩蛋按钮。原版这里会解锁隐藏主题，本版没有主题系统那套，只保留警告与文案。
    ''' </summary>
    Private Shared Sub DontClick()
        If DontClickStage = 0 Then
            If MyMsgBox("当暴露在点击确定后的场景时，有极小部分人群会引发癫痫。这种情形可能是由于某些未查出的癫痫症状引起，" &
                        "即使该人员并没有患癫痫病史也有可能造成此类病症。如果您的家人或任何家庭成员曾有过类似症状，" &
                        "请在点击确定前咨询您的医生或医师。" & vbCrLf & vbCrLf &
                        "如果您在稍后出现任何症状，包括头晕、目眩、眼部或肌肉抽搐、失去意识、失去方向感、抽搐或出现任何自己无法控制的动作，" &
                        "请立即关闭 PCL。" & vbCrLf & vbCrLf &
                        "本警告并不构成医疗建议。", "警告", "确定", "取消", IsWarn:=True) = 2 Then Return
            DontClickStage = 1
        End If
        If MyMsgBox("PCL 作者不会受理由于点击千万别点造成的任何 Bug。" & vbCrLf & vbCrLf &
                    "这是最后的警告，是否继续操作？", "警告", "继续", "算了", IsWarn:=True) = 2 Then Return
        DontClickStage = 0
        MyMsgBox("恭喜，你成功浪费了人生的几秒钟。" & vbCrLf & vbCrLf & GetRandomCave(), "然后呢？")
    End Sub

#End Region

#Region "回声洞"

    ''' <summary>
    ''' 随机取一条回声洞留言。
    ''' </summary>
    Public Shared Function GetRandomCave() As String
        If ModCaveData.CaveMessages.Length = 0 Then Return "回声洞空空如也……"
        Return RandomOne(ModCaveData.CaveMessages)
    End Function

    ''' <summary>
    ''' 启动页的「你知道吗」提示，没有自定义提示文件时用它。
    ''' </summary>
    Public Shared Function GetRandomHint() As String
        Try
            Dim HintFile As String = Paths.Base & "PCL\hints.txt"
            If File.Exists(HintFile) Then
                Dim Lines As List(Of String) = File.ReadAllLines(HintFile).
                    Where(Function(s) Not String.IsNullOrWhiteSpace(s)).ToList()
                If Lines.Any Then Return RandomOne(Lines)
            End If
        Catch ex As Exception
            Logger.Error(ex, "获取自定义提示失败")
        End Try
        Return GetRandomPresetHint()
    End Function

    Private Shared ReadOnly PresetHints As String() = {
        "在版本选择页面，右键某个版本也能进入版本设置页面！",
        "你可以在版本设置中调整分类，以将特定版本隐藏！",
        "在第一次启动游戏时，PCL 会自动将语言设置为中文！",
        "自动配置游戏内存时，PCL 会根据剩余内存与 Mod 数量动态决定分配的内存！",
        "下载前记得在设置里调整下载线程数，网络好的话多开几个会更快。"
    }

    Public Shared Function GetRandomPresetHint() As String
        Return RandomOne(PresetHints)
    End Function

#End Region

#Region "下载自定义文件"

    ''' <summary>
    ''' 用 PCL 的下载引擎下载任意链接。
    ''' </summary>
    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Try
            Url = If(Url, "").Trim()
            If Not Url.StartsWithF("http://") AndAlso Not Url.StartsWithF("https://") Then
                Hint("链接要以 http:// 或 https:// 开头", HintType.Red)
                Return
            End If

            If String.IsNullOrWhiteSpace(Folder) Then
                Dim Picked As String = Dialogs.SelectFolder("选择保存文件夹", False).FirstOrDefault
                If String.IsNullOrWhiteSpace(Picked) Then Return
                Folder = Picked
            End If

            If String.IsNullOrWhiteSpace(FileName) Then
                Dim Guess As String = Url.Split("?"c).First.TrimEnd("/"c)
                FileName = Path.GetFileName(Guess)
                If String.IsNullOrWhiteSpace(FileName) Then FileName = "download.bin"
            End If

            Folder = Folder.Replace("/", "\").TrimEnd("\"c) & "\"
            If Not Directory.Exists(Folder) Then Directory.CreateDirectory(Folder)

            Dim Task As New LoaderDownload("自定义下载文件：" & FileName, New List(Of NetFile) From {
                New NetFile({Url}, Folder & FileName, Nothing, SimulateBrowserHeaders:=True)
            })
            Dim Wrapper As New LoaderCombo(Of Integer)("自定义下载 (" & GetUuid() & ")", {Task})
            Wrapper.Start()
            LoaderTaskbarAdd(Wrapper)
            Hint("已加入下载列表：" & FileName)
        Catch ex As Exception
            Logger.Error(ex, "开始自定义下载失败")
            Hint("下载启动失败，详细信息见日志", HintType.Red)
        End Try
    End Sub

#End Region

End Class
