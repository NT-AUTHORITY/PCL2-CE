Imports PCL.Core.Controls

Public Class MyMsgLogin
    Private Data As JObject
    Private UserCode As String '需要用户在网页上输入的设备代码
    Private DeviceCode As String '用于轮询的设备代码
    Private Website As String '验证网页的网址
    Private OAuthUrl As String = "" 'OAuth 轮询验证地址


#Region "弹窗"

    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()

    Public Sub New(Converter As MyMsgBoxConverter)
        Try
            InitializeComponent()
            Btn1.Name = Btn1.Name & GetUuid()
            Btn2.Name = Btn2.Name & GetUuid()
            Btn3.Name = Btn3.Name & GetUuid()
            MyConverter = Converter
            ShapeLine.StrokeThickness = GetWPFSize(1)
            Data = Converter.Content
            OAuthUrl = Converter.AuthUrl
            Init()
        Catch ex As Exception
            Log(ex, "登录弹窗初始化失败", LogLevel.Hint)
        End Try
    End Sub

    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Try
            '动画
            Opacity = 0
            AniStart(AaColor(FrmMain.PanMsgBackground, BlurBorder.BackgroundProperty, If(MyConverter.IsWarn, New MyColor(140, 80, 0, 0), New MyColor(90, 0, 0, 0)) - FrmMain.PanMsgBackground.Background, 200), "PanMsgBackground Background")
            AniStart({
                AaOpacity(Me, 1, 120, 60),
                AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 300, 60, New AniEaseOutBack(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 300, 60, New AniEaseOutFluent(AniEasePower.Weak))
            }, "MyMsgBox " & Uuid)
            '记录日志
            Log("[Control] 登录弹窗：" & LabTitle.Text & vbCrLf & LabCaption.Text)
        Catch ex As Exception
            Log(ex, "登录弹窗加载失败", LogLevel.Hint)
        End Try
    End Sub
    Private Sub Close()
        '动画
        AniStart({
            AaCode(
            Sub()
                If Not WaitingMyMsgBox.Any() Then
                    AniStart(AaColor(FrmMain.PanMsgBackground, BlurBorder.BackgroundProperty, New MyColor(0, 0, 0, 0) - FrmMain.PanMsgBackground.Background, 200, Ease:=New AniEaseOutFluent(AniEasePower.Weak)))
                End If
            End Sub, 30),
            AaOpacity(Me, -Opacity, 80, 20),
            AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 150, 0, New AniEaseOutFluent),
            AaDouble(Sub(i) TransformRotate.Angle += i, 6 - TransformRotate.Angle, 150, 0, New AniEaseInFluent(AniEasePower.Weak)),
            AaCode(Sub() CType(Parent, Grid).Children.Remove(Me), , True)
        }, "MyMsgBox " & Uuid)
    End Sub

    '实现回车和 Esc 的接口（#4857）
    Public Sub Btn1_Click() Handles Btn1.Click
    End Sub
    Public Sub Btn3_Click() Handles Btn3.Click
        Finished(New ThreadInterruptedException)
    End Sub

    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        On Error Resume Next
        If e.GetPosition(ShapeLine).Y <= 2 Then FrmMain.DragMove()
    End Sub

#End Region

    Private Sub Finished(Result As Object)
        If MyConverter.IsExited Then Return
        MyConverter.IsExited = True
        MyConverter.Result = Result
        RunInUi(AddressOf Close)
        Thread.Sleep(200)
        'FrmMain.ShowWindowToTop()
    End Sub

    Private Sub Init()
        UserCode = Data("user_code")
        DeviceCode = Data("device_code")
        If Data("verification_uri_complete") IsNot Nothing Then 
            Website = Data("verification_uri_complete")
            LabCaption.Text = $"登录网页将自动开启，授权码将自动填充。" & vbCrLf & vbCrLf &
            $"如果网络环境不佳，网页可能一直加载不出来，届时请使用 VPN 并重试。" & vbCrLf &
            $"如果没有自动填充，请在页面内粘贴此授权码 {UserCode} （将自动复制）" & vbCrLf &
            $"你也可以用其他设备打开 {Website} 并输入授权码。"
        Else
            Website = Data("verification_uri")
            LabCaption.Text = $"登录网页将自动开启，请在网页中输入授权码 {UserCode}（将自动复制）。" & vbCrLf & vbCrLf &
            $"如果网络环境不佳，网页可能一直加载不出来，届时请使用 VPN 并重试。" & vbCrLf &
            $"你也可以用其他设备打开 {Website} 并输入上述授权码。"
        End If
        '设置 UI
        LabTitle.Text = "登录 Minecraft"
        Btn1.EventData = Website
        Btn2.EventData = UserCode
        '启动工作线程
        RunInNewThread(AddressOf WorkThread, "MyMsgLogin")
    End Sub

    Private Sub WorkThread()
        Thread.Sleep(3000)
        If MyConverter.IsExited Then Return
        OpenWebsite(Website)
        ClipboardSet(UserCode)
        Thread.Sleep((Data("interval").ToObject(Of Integer) - 1) * 1000)
        '轮询
        Dim UnknownFailureCount As Integer = 0
        Finished(0)
        Return
    End Sub

End Class
