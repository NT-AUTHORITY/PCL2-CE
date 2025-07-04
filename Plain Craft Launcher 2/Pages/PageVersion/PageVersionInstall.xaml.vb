Imports System.Text.RegularExpressions

Public Class PageVersionInstall

    Private Sub LoaderInit() Handles Me.Initialized
        DisabledPageAnimControls.Add(BtnSelectStart)
        'PageLoaderInit(LoadMinecraft, PanLoad, PanBack, Nothing, DlClientListLoader, AddressOf LoadMinecraft_OnFinish)
        PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, Nothing, DlClientListLoader, AddressOf GetCurrentInfo)
    End Sub

    Private IsLoad As Boolean = False
    Private LastVersionName As String = Nothing
    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()

        GetCurrentInfo()

        Dim NeedRefresh = LastVersionName Is Nothing OrElse LastVersionName <> SelectedMinecraftId
        LastVersionName = SelectedMinecraftId

        DlOptiFineListLoader.Start(IsForceRestart:=NeedRefresh)
        DlLiteLoaderListLoader.Start(IsForceRestart:=NeedRefresh)
        DlFabricListLoader.Start(IsForceRestart:=NeedRefresh)
        DlQuiltListLoader.Start(IsForceRestart:=NeedRefresh)
        DlNeoForgeListLoader.Start(IsForceRestart:=NeedRefresh)
        DlCleanroomListLoader.Start(IsForceRestart:=NeedRefresh)
        DlLabyModListLoader.Start(IsForceRestart:=NeedRefresh)

        '重载预览
        SelectReload()

        '非重复加载部分
        If IsLoad Then Exit Sub
        IsLoad = True

        McDownloadForgeRecommendedRefresh()

        LoadOptiFine.State = DlOptiFineListLoader
        LoadLiteLoader.State = DlLiteLoaderListLoader
        LoadFabric.State = DlFabricListLoader
        LoadFabricApi.State = DlFabricApiLoader
        LoadQuilt.State = DlQuiltListLoader
        LoadQSL.State = DlQSLLoader
        LoadNeoForge.State = DlNeoForgeListLoader
        LoadCleanroom.State = DlCleanroomListLoader
        LoadOptiFabric.State = DlOptiFabricLoader
        LoadLabyMod.State = DlLabyModListLoader
    End Sub

#Region "页面切换"

    '页面切换动画
    Public IsInSelectPage As Boolean = False
    Private IsFirstLoaded As Boolean = False
    Private Sub EnterSelectPage()
        If IsInSelectPage Then Exit Sub
        IsInSelectPage = True

        DisabledPageAnimControls.Remove(BtnSelectStart)
        BtnSelectStart.Show = True
        AutoSelectedFabricApi = False
        AutoSelectedQSL = False
        AutoSelectedOptiFabric = False
        IsSelectNameEdited = False
        PanSelect.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = True
        PanMinecraft.IsHitTestVisible = False
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        CardMinecraft.IsSwaped = True
        CardOptiFine.IsSwaped = True
        CardLiteLoader.IsSwaped = True
        CardForge.IsSwaped = True
        CardNeoForge.IsSwaped = True
        CardCleanroom.IsSwaped = True
        CardFabric.IsSwaped = True
        CardFabricApi.IsSwaped = True
        CardQuilt.IsSwaped = True
        CardQSL.IsSwaped = True
        CardOptiFabric.IsSwaped = True
        CardLabyMod.IsSwaped = True

        If Not Setup.Get("HintInstallBack") Then
            Setup.Set("HintInstallBack", True)
            Hint("点击 Minecraft 项即可返回游戏主版本选择页面！")
        End If

        '如果在选择页面按了刷新键，选择页的东西可能会由于动画被隐藏，但不会由于加载结束而再次显示，因此这里需要手动恢复
        For Each Card In GetAllAnimControls(PanSelect)
            Card.Opacity = 1
            Card.RenderTransform = New TranslateTransform
        Next

        '启动 Forge 加载
        If SelectedMinecraftId.StartsWith("1.") Then
            Dim ForgeLoader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion " & SelectedMinecraftId, AddressOf DlForgeVersionMain)
            LoadForge.State = ForgeLoader
            ForgeLoader.Start(SelectedMinecraftId)
        End If

        '启动 Fabric API、QSL、OptiFabric、LabyMod 加载
        DlFabricApiLoader.Start()
        DlQSLLoader.Start()
        DlOptiFabricLoader.Start()

        AniStart({
            AaOpacity(PanMinecraft, -PanMinecraft.Opacity, 100, 10),
            AaCode(
            Sub()
                PanBack.ScrollToHome()
                OptiFine_Loaded()
                LiteLoader_Loaded()
                Forge_Loaded()
                NeoForge_Loaded()
                Cleanroom_Loaded()
                Fabric_Loaded()
                FabricApi_Loaded()
                Quilt_Loaded()
                QSL_Loaded()
                LabyMod_Loaded()
                OptiFabric_Loaded()
                SelectReload()
            End Sub, After:=True),
            AaOpacity(PanSelect, 1 - PanSelect.Opacity, 250, 150),
            AaCode(
            Sub()
                PanMinecraft.Visibility = Visibility.Collapsed
                PanBack.IsHitTestVisible = True
                '初始化 Binding
                If IsFirstLoaded Then Exit Sub
                IsFirstLoaded = True
                BtnOptiFineClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardOptiFine.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnLiteLoaderClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardLiteLoader.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnForgeClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardForge.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnNeoForgeClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardNeoForge.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnCleanroomClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardCleanroom.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnFabricClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardFabric.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnFabricApiClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardFabricApi.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnQuiltClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardQuilt.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnQSLClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardQSL.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnLabyModClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardLabyMod.MainTextBlock, .Mode = BindingMode.OneWay})
                BtnOptiFabricClearInner.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = CardOptiFabric.MainTextBlock, .Mode = BindingMode.OneWay})
            End Sub,, True)
        }, "FrmVersionInstall SelectPageSwitch", True)
    End Sub
    Public Sub ExitSelectPage()
        If Not IsInSelectPage Then Exit Sub
        IsInSelectPage = False

        LoadMinecraft_OnFinish()

        DisabledPageAnimControls.Add(BtnSelectStart)
        BtnSelectStart.Show = False

        SelectClear() '清除已选择项
        PanMinecraft.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = False
        PanMinecraft.IsHitTestVisible = True
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        AniStart({
            AaOpacity(PanSelect, -PanSelect.Opacity, 90, 10),
            AaCode(Sub() PanBack.ScrollToHome(), After:=True),
            AaOpacity(PanMinecraft, 1 - PanMinecraft.Opacity, 150, 100),
            AaCode(Sub()
                       PanSelect.Visibility = Visibility.Collapsed
                       PanBack.IsHitTestVisible = True
                   End Sub,, True)
        }, "FrmVersionInstall SelectPageSwitch")
    End Sub

    '页面切换触发
    Public Sub MinecraftSelected(sender As MyListItem, e As MouseButtonEventArgs)
        SelectClear()
        SelectedMinecraftId = sender.Title
        SelectedMinecraftJsonUrl = sender.Tag("url").ToString
        SelectedMinecraftIcon = sender.Logo
        EnterSelectPage()
    End Sub
    Private Sub CardMinecraft_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardMinecraft.PreviewSwap
        ExitSelectPage()
        e.Handled = True
    End Sub

#End Region

#Region "选择"

    'Minecraft
    Private SelectedMinecraftId As String
    Private SelectedMinecraftJsonUrl As String
    Private SelectedMinecraftIcon As String

    'OptiFine
    Private SelectedOptiFine As DlOptiFineListEntry = Nothing
    Private Sub SetOptiFineInfoShow(IsShow As String)
        If PanOptiFineInfo.Tag = IsShow Then Exit Sub
        PanOptiFineInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanOptiFineInfo, -CType(PanOptiFineInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanOptiFineInfo, 1 - PanOptiFineInfo.Opacity, 100, 90)
            }, "SetOptiFineInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanOptiFineInfo, 6 - CType(PanOptiFineInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanOptiFineInfo, -PanOptiFineInfo.Opacity, 100)
            }, "SetOptiFineInfoShow")
        End If
    End Sub

    ''' <summary>
    ''' 选定的 Mod Loader 名称，内容应为 Forge / NeoForge / Fabric / Quilt / Cleanroom / LabyMod
    ''' </summary>
    Private SelectedLoaderName As String = Nothing

    ''' <summary>
    ''' 选定的 Mod Loader API 名称，内容应为 Fabric API 或 QFAPI / QSL
    ''' </summary>
    Private SelectedAPIName As String = Nothing

    'LiteLoader
    Private SelectedLiteLoader As DlLiteLoaderListEntry = Nothing
    Private Sub SetLiteLoaderInfoShow(IsShow As String)
        If PanLiteLoaderInfo.Tag = IsShow Then Exit Sub
        PanLiteLoaderInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanLiteLoaderInfo, -CType(PanLiteLoaderInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanLiteLoaderInfo, 1 - PanLiteLoaderInfo.Opacity, 100, 90)
            }, "SetLiteLoaderInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanLiteLoaderInfo, 6 - CType(PanLiteLoaderInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanLiteLoaderInfo, -PanLiteLoaderInfo.Opacity, 100)
            }, "SetLiteLoaderInfoShow")
        End If
    End Sub

    'Forge
    Private SelectedForge As DlForgeVersionEntry = Nothing
    Private Sub SetForgeInfoShow(IsShow As String)
        If PanForgeInfo.Tag = IsShow Then Exit Sub
        PanForgeInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanForgeInfo, -CType(PanForgeInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanForgeInfo, 1 - PanForgeInfo.Opacity, 100, 90)
            }, "SetForgeInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanForgeInfo, 6 - CType(PanForgeInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanForgeInfo, -PanForgeInfo.Opacity, 100)
            }, "SetForgeInfoShow")
        End If
    End Sub

    'Cleanroom
    Private SelectedCleanroom As DlCleanroomListEntry = Nothing
    Private SelectedCleanroomVersion As String = Nothing
    Private Sub SetCleanroomInfoShow(IsShow As String)
        If PanCleanroomInfo.Tag = IsShow Then Exit Sub
        PanCleanroomInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanCleanroomInfo, -CType(PanCleanroomInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanCleanroomInfo, 1 - PanCleanroomInfo.Opacity, 100, 90)
            }, "SetCleanroomInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanCleanroomInfo, 6 - CType(PanCleanroomInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanCleanroomInfo, -PanCleanroomInfo.Opacity, 100)
            }, "SetCleanroomInfoShow")
        End If
    End Sub

    'NeoForge
    Private SelectedNeoForge As DlNeoForgeListEntry = Nothing
    Private SelectedNeoForgeVersion As String = Nothing
    Private Sub SetNeoForgeInfoShow(IsShow As String)
        If PanNeoForgeInfo.Tag = IsShow Then Exit Sub
        PanNeoForgeInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanNeoForgeInfo, -CType(PanNeoForgeInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanNeoForgeInfo, 1 - PanNeoForgeInfo.Opacity, 100, 90)
            }, "SetNeoForgeInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanNeoForgeInfo, 6 - CType(PanNeoForgeInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanNeoForgeInfo, -PanNeoForgeInfo.Opacity, 100)
            }, "SetNeoForgeInfoShow")
        End If
    End Sub

    'Fabric
    Private SelectedFabric As String = Nothing
    Private Sub SetFabricInfoShow(IsShow As String)
        If PanFabricInfo.Tag = IsShow Then Exit Sub
        PanFabricInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanFabricInfo, -CType(PanFabricInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanFabricInfo, 1 - PanFabricInfo.Opacity, 100, 90)
            }, "SetFabricInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanFabricInfo, 6 - CType(PanFabricInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanFabricInfo, -PanFabricInfo.Opacity, 100)
            }, "SetFabricInfoShow")
        End If
    End Sub

    'FabricApi
    Private SelectedFabricApi As CompFile = Nothing
    Private Sub SetFabricApiInfoShow(IsShow As String)
        If PanFabricApiInfo.Tag = IsShow Then Exit Sub
        PanFabricApiInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanFabricApiInfo, -CType(PanFabricApiInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanFabricApiInfo, 1 - PanFabricApiInfo.Opacity, 100, 90)
            }, "SetFabricApiInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanFabricApiInfo, 6 - CType(PanFabricApiInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanFabricApiInfo, -PanFabricApiInfo.Opacity, 100)
            }, "SetFabricApiInfoShow")
        End If
    End Sub

    'Quilt
    Private SelectedQuilt As String = Nothing
    Private Sub SetQuiltInfoShow(IsShow As String)
        If PanQuiltInfo.Tag = IsShow Then Exit Sub
        PanQuiltInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanQuiltInfo, -CType(PanQuiltInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanQuiltInfo, 1 - PanQuiltInfo.Opacity, 100, 90)
            }, "SetQuiltInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanQuiltInfo, 6 - CType(PanQuiltInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanQuiltInfo, -PanQuiltInfo.Opacity, 100)
            }, "SetQuiltInfoShow")
        End If
    End Sub

    'QSL
    Private SelectedQSL As CompFile = Nothing
    Private Sub SetQSLInfoShow(IsShow As String)
        If PanQSLInfo.Tag = IsShow Then Exit Sub
        PanQSLInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanQSLInfo, -CType(PanQSLInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanQSLInfo, 1 - PanQSLInfo.Opacity, 100, 90)
            }, "SetQSLInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanQSLInfo, 6 - CType(PanQSLInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanQSLInfo, -PanQSLInfo.Opacity, 100)
            }, "SetQSLInfoShow")
        End If
    End Sub

    'LabyMod
    Private SelectedLabyModChannel As String = Nothing
    Private SelectedLabyModCommitRef As String = Nothing
    Private SelectedLabyModVersion As String = Nothing
    Private Sub SetLabyModInfoShow(IsShow As String)
        If PanLabyModInfo.Tag = IsShow Then Exit Sub
        PanLabyModInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanLabyModInfo, -CType(PanLabyModInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanLabyModInfo, 1 - PanLabyModInfo.Opacity, 100, 90)
            }, "SetLabyModInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanLabyModInfo, 6 - CType(PanLabyModInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanLabyModInfo, -PanLabyModInfo.Opacity, 100)
            }, "SetLabyModInfoShow")
        End If
    End Sub

    'OptiFabric
    Private SelectedOptiFabric As CompFile = Nothing
    Private Sub SetOptiFabricInfoShow(IsShow As String)
        If PanOptiFabricInfo.Tag = IsShow Then Exit Sub
        PanOptiFabricInfo.Tag = IsShow
        If IsShow = "True" Then
            '显示信息栏
            AniStart({
                AaTranslateY(PanOptiFabricInfo, -CType(PanOptiFabricInfo.RenderTransform, TranslateTransform).Y, 270, 100, Ease:=New AniEaseOutBack),
                AaOpacity(PanOptiFabricInfo, 1 - PanOptiFabricInfo.Opacity, 100, 90)
            }, "SetOptiFabricInfoShow")
        Else
            '隐藏信息栏
            AniStart({
                AaTranslateY(PanOptiFabricInfo, 6 - CType(PanOptiFabricInfo.RenderTransform, TranslateTransform).Y, 200),
                AaOpacity(PanOptiFabricInfo, -PanOptiFabricInfo.Opacity, 100)
            }, "SetOptiFabricInfoShow")
        End If
    End Sub

    '其他项目
    Private InstalledOtherLoader As String = Nothing
    Private InstalledOtherInfo As String = Nothing

    Private IsReloading As Boolean = False '#3742 中，LoadOptiFineGetError 会初始化 LoadOptiFine，触发事件 LoadOptiFine.StateChanged，导致再次调用 SelectReload
    ''' <summary>
    ''' 重载已选择的项目的显示。
    ''' </summary>
    Private Sub SelectReload() Handles CardOptiFine.Swap, LoadOptiFine.StateChanged, CardForge.Swap, LoadForge.StateChanged, CardNeoForge.Swap, LoadNeoForge.StateChanged, CardFabric.Swap, LoadFabric.StateChanged, CardFabricApi.Swap, LoadFabricApi.StateChanged, CardOptiFabric.Swap, LoadOptiFabric.StateChanged, CardLiteLoader.Swap, LoadLiteLoader.StateChanged, LoadQuilt.StateChanged, CardQuilt.Swap, LoadQSL.StateChanged, CardQSL.Swap, LoadCleanroom.StateChanged, CardCleanroom.Swap, LoadLabyMod.StateChanged, CardLabyMod.Swap
        If SelectedMinecraftId Is Nothing OrElse IsReloading Then Exit Sub
        IsReloading = True
        Dim SelectedInfo As String = GetSelectInfo()
        '主预览
        ItemSelect.Title = PageVersionLeft.Version.Name
        ItemSelect.Logo = GetSelectLogo()
        BtnSelectStart.IsEnabled = True
        If SelectedInfo = CurrentInfo Then
            ItemSelect.Info = SelectedInfo
            BtnSelectStart.Text = "开始重置"
            BtnSelectStart.Logo = Logo.IconButtonReset
        Else
            ItemSelect.Info = CurrentInfo + " → " + SelectedInfo
            BtnSelectStart.Text = "开始修改"
            BtnSelectStart.Logo = Logo.IconButtonEdit
        End If
        'Minecraft
        LabMinecraft.Text = SelectedMinecraftId
        ImgMinecraft.Source = New MyBitmap(SelectedMinecraftIcon)
        'OptiFine
        Dim OptiFineError As String = LoadOptiFineGetError()
        CardOptiFine.MainSwap.Visibility = If(OptiFineError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If OptiFineError IsNot Nothing Then CardOptiFine.IsSwaped = True '例如在同时展开卡片时选择了不兼容项则强制折叠
        SetOptiFineInfoShow(CardOptiFine.IsSwaped)
        If SelectedOptiFine Is Nothing Then
            BtnOptiFineClear.Visibility = Visibility.Collapsed
            ImgOptiFine.Visibility = Visibility.Collapsed
            LabOptiFine.Text = If(OptiFineError, "可以添加")
            LabOptiFine.Foreground = ColorGray4
        Else
            BtnOptiFineClear.Visibility = Visibility.Visible
            ImgOptiFine.Visibility = Visibility.Visible
            LabOptiFine.Text = SelectedOptiFine.NameDisplay.Replace(SelectedMinecraftId & " ", "")
            LabOptiFine.Foreground = ColorGray1
        End If
        'LiteLoader
        If Not SelectedMinecraftId.Contains("1.") OrElse Val(SelectedMinecraftId.Split(".")(1)) > 12 Then
            CardLiteLoader.Visibility = Visibility.Collapsed
        Else
            CardLiteLoader.Visibility = Visibility.Visible
            Dim LiteLoaderError As String = LoadLiteLoaderGetError()
            CardLiteLoader.MainSwap.Visibility = If(LiteLoaderError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If LiteLoaderError IsNot Nothing Then CardLiteLoader.IsSwaped = True '例如在同时展开卡片时选择了不兼容项则强制折叠
            SetLiteLoaderInfoShow(CardLiteLoader.IsSwaped)
            If SelectedLiteLoader Is Nothing Then
                BtnLiteLoaderClear.Visibility = Visibility.Collapsed
                ImgLiteLoader.Visibility = Visibility.Collapsed
                LabLiteLoader.Text = If(LiteLoaderError, "可以添加")
                LabLiteLoader.Foreground = ColorGray4
            Else
                BtnLiteLoaderClear.Visibility = Visibility.Visible
                ImgLiteLoader.Visibility = Visibility.Visible
                LabLiteLoader.Text = SelectedLiteLoader.Inherit
                LabLiteLoader.Foreground = ColorGray1
            End If
        End If
        'Forge
        Dim ForgeError As String = LoadForgeGetError()
        CardForge.MainSwap.Visibility = If(ForgeError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If ForgeError IsNot Nothing Then CardForge.IsSwaped = True
        SetForgeInfoShow(CardForge.IsSwaped)
        If SelectedForge Is Nothing Then
            BtnForgeClear.Visibility = Visibility.Collapsed
            ImgForge.Visibility = Visibility.Collapsed
            LabForge.Text = If(ForgeError, "可以添加")
            LabForge.Foreground = ColorGray4
        Else
            BtnForgeClear.Visibility = Visibility.Visible
            ImgForge.Visibility = Visibility.Visible
            LabForge.Text = SelectedForge.VersionName
            LabForge.Foreground = ColorGray1
        End If
        'Cleanroom
        If SelectedMinecraftId = "1.12.2" Then
            CardCleanroom.Visibility = Visibility.Visible
            Dim CleanroomError As String = LoadCleanroomGetError()
            CardCleanroom.MainSwap.Visibility = If(CleanroomError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If CleanroomError IsNot Nothing Then CardCleanroom.IsSwaped = True
            SetCleanroomInfoShow(CardCleanroom.IsSwaped)
            If SelectedCleanroom Is Nothing AndAlso SelectedCleanroomVersion = Nothing Then
                BtnCleanroomClear.Visibility = Visibility.Collapsed
                ImgCleanroom.Visibility = Visibility.Collapsed
                LabCleanroom.Text = If(CleanroomError, "可以添加")
                LabCleanroom.Foreground = ColorGray4
            Else
                BtnCleanroomClear.Visibility = Visibility.Visible
                ImgCleanroom.Visibility = Visibility.Visible
                LabCleanroom.Text = If(SelectedCleanroom IsNot Nothing, SelectedCleanroom.VersionName, SelectedCleanroomVersion)
                LabCleanroom.Foreground = ColorGray1
            End If
        Else
            CardCleanroom.Visibility = Visibility.Collapsed
        End If
        'NeoForge
        If Not SelectedMinecraftId.Contains("1.") OrElse Val(SelectedMinecraftId.Split(".")(1)) <= 19 Then
            CardNeoForge.Visibility = Visibility.Collapsed
        Else
            CardNeoForge.Visibility = Visibility.Visible
            Dim NeoForgeError As String = LoadNeoForgeGetError()
            CardNeoForge.MainSwap.Visibility = If(NeoForgeError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If NeoForgeError IsNot Nothing Then CardNeoForge.IsSwaped = True
            SetNeoForgeInfoShow(CardNeoForge.IsSwaped)
            If SelectedNeoForge Is Nothing AndAlso SelectedNeoForgeVersion = Nothing Then
                BtnNeoForgeClear.Visibility = Visibility.Collapsed
                ImgNeoForge.Visibility = Visibility.Collapsed
                LabNeoForge.Text = If(NeoForgeError, "可以添加")
                LabNeoForge.Foreground = ColorGray4
            Else
                BtnNeoForgeClear.Visibility = Visibility.Visible
                ImgNeoForge.Visibility = Visibility.Visible
                LabNeoForge.Text = If(SelectedNeoForge IsNot Nothing, SelectedNeoForge.VersionName, SelectedNeoForgeVersion)
                LabNeoForge.Foreground = ColorGray1
            End If
        End If
        'Fabric
        If SelectedMinecraftId.Contains("1.") AndAlso Val(SelectedMinecraftId.Split(".")(1)) <= 13 Then
            CardFabric.Visibility = Visibility.Collapsed
        Else
            CardFabric.Visibility = Visibility.Visible
            Dim FabricError As String = LoadFabricGetError()
            CardFabric.MainSwap.Visibility = If(FabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If FabricError IsNot Nothing Then CardFabric.IsSwaped = True
            SetFabricInfoShow(CardFabric.IsSwaped)
            If SelectedFabric Is Nothing Then
                BtnFabricClear.Visibility = Visibility.Collapsed
                ImgFabric.Visibility = Visibility.Collapsed
                LabFabric.Text = If(FabricError, "可以添加")
                LabFabric.Foreground = ColorGray4
            Else
                BtnFabricClear.Visibility = Visibility.Visible
                ImgFabric.Visibility = Visibility.Visible
                LabFabric.Text = SelectedFabric.Replace("+build", "")
                LabFabric.Foreground = ColorGray1
            End If
        End If
        'FabricApi
        If SelectedFabric Is Nothing AndAlso SelectedQuilt Is Nothing Then
            CardFabricApi.Visibility = Visibility.Collapsed
        Else
            CardFabricApi.Visibility = Visibility.Visible
            Dim FabricApiError As String = LoadFabricApiGetError()
            CardFabricApi.MainSwap.Visibility = If(FabricApiError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If FabricApiError IsNot Nothing OrElse SelectedFabric Is Nothing AndAlso SelectedQuilt Is Nothing Then CardFabricApi.IsSwaped = True
            SetFabricApiInfoShow(CardFabricApi.IsSwaped)
            If SelectedFabricApi Is Nothing Then
                BtnFabricApiClear.Visibility = Visibility.Collapsed
                ImgFabricApi.Visibility = Visibility.Collapsed
                LabFabricApi.Text = If(FabricApiError, "可以添加")
                LabFabricApi.Foreground = ColorGray4
            Else
                BtnFabricApiClear.Visibility = Visibility.Visible
                ImgFabricApi.Visibility = Visibility.Visible
                LabFabricApi.Text = SelectedFabricApi.DisplayName.Split("]")(1).Replace("Fabric API ", "").Replace(" build ", ".").Split("+").First.Trim
                LabFabricApi.Foreground = ColorGray1
            End If
        End If
        'Quilt
        If SelectedMinecraftId.Contains("1.") AndAlso Val(SelectedMinecraftId.Split(".")(1)) <= 14 AndAlso Not SelectedMinecraftId.Contains("1.14.4") Then
            CardQuilt.Visibility = Visibility.Collapsed
        Else
            CardQuilt.Visibility = Visibility.Visible
            Dim QuiltError As String = LoadQuiltGetError()
            CardQuilt.MainSwap.Visibility = If(QuiltError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If QuiltError IsNot Nothing Then CardQuilt.IsSwaped = True
            SetQuiltInfoShow(CardQuilt.IsSwaped)
            If SelectedQuilt Is Nothing Then
                BtnQuiltClear.Visibility = Visibility.Collapsed
                ImgQuilt.Visibility = Visibility.Collapsed
                LabQuilt.Text = If(QuiltError, "可以添加")
                LabQuilt.Foreground = ColorGray4
            Else
                BtnQuiltClear.Visibility = Visibility.Visible
                ImgQuilt.Visibility = Visibility.Visible
                LabQuilt.Text = SelectedQuilt.Replace("+build", "")
                LabQuilt.Foreground = ColorGray1
            End If
        End If
        'QSL
        If SelectedQuilt Is Nothing Then
            CardQSL.Visibility = Visibility.Collapsed
        Else
            CardQSL.Visibility = Visibility.Visible
            Dim QSLError As String = LoadQSLGetError()
            CardQSL.MainSwap.Visibility = If(QSLError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If QSLError IsNot Nothing OrElse SelectedQuilt Is Nothing Then CardQSL.IsSwaped = True
            SetQSLInfoShow(CardQSL.IsSwaped)
            If SelectedQSL Is Nothing Then
                BtnQSLClear.Visibility = Visibility.Collapsed
                ImgQSL.Visibility = Visibility.Collapsed
                LabQSL.Text = If(QSLError, "可以添加")
                LabQSL.Foreground = ColorGray4
            Else
                BtnQSLClear.Visibility = Visibility.Visible
                ImgQSL.Visibility = Visibility.Visible
                LabQSL.Text = SelectedQSL.DisplayName.Split("]")(1).Trim
                LabQSL.Foreground = ColorGray1
            End If
        End If
        'LabyMod
        If SelectedMinecraftId.Contains("1.") AndAlso Val(SelectedMinecraftId.Split(".")(1)) <= 8 Then
            CardLabyMod.Visibility = Visibility.Collapsed
        Else
            CardLabyMod.Visibility = Visibility.Visible
            Dim LabyModError As String = LoadLabyModGetError()
            CardLabyMod.MainSwap.Visibility = If(LabyModError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If LabyModError IsNot Nothing Then CardLabyMod.IsSwaped = True
            SetLabyModInfoShow(CardLabyMod.IsSwaped)
            If SelectedLabyModVersion Is Nothing Then
                BtnLabyModClear.Visibility = Visibility.Collapsed
                ImgLabyMod.Visibility = Visibility.Collapsed
                LabLabyMod.Text = If(LabyModError, "可以添加")
                LabLabyMod.Foreground = ColorGray4
            Else
                BtnLabyModClear.Visibility = Visibility.Visible
                ImgLabyMod.Visibility = Visibility.Visible
                LabLabyMod.Text = SelectedLabyModVersion
                LabLabyMod.Foreground = ColorGray1
            End If
        End If
        'OptiFabric
        If SelectedFabric Is Nothing OrElse SelectedOptiFine Is Nothing Then
            CardOptiFabric.Visibility = Visibility.Collapsed
        Else
            CardOptiFabric.Visibility = Visibility.Visible
            Dim OptiFabricError As String = LoadOptiFabricGetError()
            CardOptiFabric.MainSwap.Visibility = If(OptiFabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If OptiFabricError IsNot Nothing OrElse SelectedFabric Is Nothing Then CardOptiFabric.IsSwaped = True
            SetOptiFabricInfoShow(CardOptiFabric.IsSwaped)
            If SelectedOptiFabric Is Nothing Then
                BtnOptiFabricClear.Visibility = Visibility.Collapsed
                ImgOptiFabric.Visibility = Visibility.Collapsed
                LabOptiFabric.Text = If(OptiFabricError, "可以添加")
                LabOptiFabric.Foreground = ColorGray4
            Else
                BtnOptiFabricClear.Visibility = Visibility.Visible
                ImgOptiFabric.Visibility = Visibility.Visible
                LabOptiFabric.Text = SelectedOptiFabric.DisplayName.ToLower.Replace("optifabric-", "").Replace(".jar", "").Trim.TrimStart("v")
                LabOptiFabric.Foreground = ColorGray1
            End If
        End If
        '主警告
        If SelectedFabric IsNot Nothing AndAlso SelectedFabricApi Is Nothing Then
            HintFabricAPI.Visibility = Visibility.Visible
        Else
            HintFabricAPI.Visibility = Visibility.Collapsed
        End If
        If SelectedQuilt IsNot Nothing AndAlso SelectedQSL Is Nothing AndAlso SelectedFabricApi Is Nothing Then
            HintQSL.Visibility = Visibility.Visible
        Else
            HintQSL.Visibility = Visibility.Collapsed
        End If
        If SelectedQuilt IsNot Nothing AndAlso SelectedFabricApi IsNot Nothing AndAlso DlQSLLoader.Output IsNot Nothing Then
            For Each Version In DlQSLLoader.Output
                If IsSuitableQSL(Version.GameVersions, SelectedMinecraftId) Then
                    HintQuiltFabricAPI.Visibility = Visibility.Visible
                    Exit For
                Else
                    HintQuiltFabricAPI.Visibility = Visibility.Collapsed
                End If
            Next
        Else
            HintQuiltFabricAPI.Visibility = Visibility.Collapsed
        End If
        If SelectedFabric IsNot Nothing AndAlso SelectedOptiFine IsNot Nothing AndAlso SelectedOptiFabric Is Nothing Then
            If SelectedMinecraftId.StartsWith("1.14") OrElse SelectedMinecraftId.StartsWith("1.15") Then
                HintOptiFabric.Visibility = Visibility.Collapsed
                HintOptiFabricOld.Visibility = Visibility.Visible
            Else
                HintOptiFabric.Visibility = Visibility.Visible
                HintOptiFabricOld.Visibility = Visibility.Collapsed
            End If
        Else
            HintOptiFabric.Visibility = Visibility.Collapsed
            HintOptiFabricOld.Visibility = Visibility.Collapsed
        End If
        If SelectedMinecraftId.Contains("1.") AndAlso Val(SelectedMinecraftId.Split(".")(1)) >= 16 AndAlso SelectedOptiFine IsNot Nothing AndAlso
           (SelectedForge IsNot Nothing OrElse SelectedFabric IsNot Nothing) Then
            HintModOptiFine.Visibility = Visibility.Visible
        Else
            HintModOptiFine.Visibility = Visibility.Collapsed
        End If
        '结束
        IsReloading = False
    End Sub
    ''' <summary>
    ''' 清空已选择的项目。
    ''' </summary>
    Private Sub SelectClear()
        SelectedMinecraftId = Nothing
        SelectedMinecraftJsonUrl = Nothing
        SelectedMinecraftIcon = Nothing
        SelectedOptiFine = Nothing
        SelectedLiteLoader = Nothing
        SelectedLoaderName = Nothing
        SelectedAPIName = Nothing
        SelectedForge = Nothing
        SelectedNeoForge = Nothing
        SelectedNeoForgeVersion = Nothing
        SelectedCleanroom = Nothing
        SelectedCleanroomVersion = Nothing
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        SelectedQuilt = Nothing
        SelectedQSL = Nothing
        SelectedOptiFabric = Nothing
        SelectedLabyModCommitRef = Nothing
        SelectedLabyModVersion = Nothing
        SelectedLabyModChannel = Nothing
    End Sub

    '显示信息获取
    ''' <summary>
    ''' 获取版本描述信息。
    ''' </summary>
    Private Function GetSelectInfo() As String
        Dim Info As String = ""
        Info += SelectedMinecraftId
        If SelectedFabric IsNot Nothing Then
            Info += ", Fabric " & SelectedFabric.Replace("+build", "")
        End If
        If SelectedQuilt IsNot Nothing Then
            Info += ", Quilt " & SelectedQuilt
        End If
        If SelectedForge IsNot Nothing Then
            Info += ", Forge " & SelectedForge.VersionName
        End If
        If SelectedNeoForge IsNot Nothing OrElse Not SelectedNeoForgeVersion = Nothing Then
            Info += ", NeoForge " & If(SelectedNeoForge IsNot Nothing, SelectedNeoForge.VersionName, SelectedNeoForgeVersion)
        End If
        If SelectedCleanroom IsNot Nothing OrElse Not SelectedCleanroomVersion = Nothing Then
            Info += ", Cleanroom " & If(SelectedCleanroom IsNot Nothing, SelectedCleanroom.VersionName, SelectedCleanroomVersion)
        End If
        If SelectedLabyModVersion IsNot Nothing Then
            Info += ", LabyMod " & SelectedLabyModVersion
        End If
        If SelectedLiteLoader IsNot Nothing Then
            Info += ", LiteLoader"
        End If
        If SelectedOptiFine IsNot Nothing Then
            Info += ", OptiFine " & SelectedOptiFine.NameDisplay.Replace(SelectedMinecraftId & " ", "")
        End If
        If InstalledOtherLoader IsNot Nothing Then
            Info += $", {InstalledOtherLoader} {InstalledOtherInfo}"
        End If
        If Info = SelectedMinecraftId Then Info += ", 无附加安装"
        Return Info.TrimStart(", ".ToCharArray())
    End Function
    ''' <summary>
    ''' 获取版本图标。
    ''' </summary>
    Private Function GetSelectLogo() As String
        If SelectedFabric IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Fabric.png"
        ElseIf SelectedQuilt IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Quilt.png"
        ElseIf SelectedForge IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Anvil.png"
        ElseIf SelectedNeoForge IsNot Nothing OrElse Not SelectedNeoForgeVersion = Nothing Then
            Return "pack://application:,,,/images/Blocks/NeoForge.png"
        ElseIf SelectedCleanroom IsNot Nothing OrElse Not SelectedCleanroomVersion = Nothing Then
            Return "pack://application:,,,/images/Blocks/Cleanroom.png"
        ElseIf SelectedLiteLoader IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Egg.png"
        ElseIf SelectedOptiFine IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/GrassPath.png"
        ElseIf SelectedLabyModVersion IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/LabyMod.png"
        Else
            Return SelectedMinecraftIcon
        End If
    End Function

    '版本名处理
    Private IsSelectNameEdited As Boolean = False
    Private IsSelectNameChanging As Boolean = False
    
    Private Shared ReadOnly RegexIsJarFile As New Regex("\.jar(\.disabled)?$")
    
    ''' <summary>
    ''' 通过文件名关键字和 mod id 比如 <c>fabric</c> <c>api</c> 和 <c>fabric-api</c> 来获取给定版本 mods 目录中某个 mod 的 <see cref="LocalCompFile"/> 对象
    ''' <br />
    ''' <b>为了不浪费性能，关键字统一用小写</b> 
    ''' </summary>
    ''' <returns>
    ''' 如果文件名包含主关键字，以及其他关键字中的任意一个，同时 mod id 一致，即认为匹配，返回对应的对象，若没有匹配的文件则返回空值。
    ''' </returns>
    Private Shared Function GetModLocalCompByKeywords(modId As String, mainKeyword As String, ParamArray keywords As String()) As LocalCompFile
        If modId Is Nothing Then Return Nothing
        Return GetModLocalCompByKeywords({ modId }, mainKeyword, keywords)
    End Function
    
    Private Shared Function GetModLocalCompByKeywords(modIds As String(), mainKeyword As String, ParamArray keywords As String()) As LocalCompFile
        Dim version = PageVersionLeft.Version
        If Not version.Modable Then Return Nothing '跳过不可安装 mod 版本
        Dim modFolder = $"{version.Path}mods"
        If Not Directory.Exists(modFolder) Then Return Nothing '确保 mods 目录存在
        For Each file In Directory.EnumerateFiles(modFolder, $"*{mainKeyword}*")
            Dim lowerFilePath = file.ToLower() '统一转为小写
            If Not RegexIsJarFile.IsMatch(lowerFilePath) Then Continue For '检查是否是 jar 文件
            If keywords.Length > 0 And Not keywords.Any(Function(keyword) lowerFilePath.Contains(keyword)) Then Continue For '检查是否包含关键字
            Dim localComp = New LocalCompFile(file)
            localComp.Load()
            If (modIds.Any(Function(modId) localComp.ModId = modId)) Then Return localComp
        Next
        Return Nothing
    End Function
    
    Private _currentFabricApi As CompFile = Nothing '加载完成后直接调用以提高性能
    Private _currentFabricApiPath As String = Nothing
    Private Function GetCurrentFabricApi() '进入页面和联网加载时调用
        Dim loaderOutput = DlFabricApiLoader.Output
        If loaderOutput Is Nothing Then Return Nothing '确保联网信息已加载
        Dim localComp = GetModLocalCompByKeywords({ "fabric-api", "fabric" }, "fabric", "api")
        If localComp Is Nothing Then Return Nothing
        Dim result = loaderOutput.FirstOrDefault(Function(comp) comp.Hash = localComp.ModrinthHash)
        If result IsNot Nothing Then
            _currentFabricApi = result
            _currentFabricApiPath = localComp.Path
        End If
        Return result
    End Function
    
    Private _currentQsl As CompFile = Nothing
    Private _currentQslPath As String = Nothing
    Private Function GetCurrentQsl()
        Dim loaderOutput = DlQslLoader.Output
        If loaderOutput Is Nothing Then Return Nothing
        Dim localComp = GetModLocalCompByKeywords("quilted_fabric_api", "qsl", "qf", "fabric", "api")
        '兼容测试版的文件名 没错这玩意测试版命名方式甚至与正式版不一样
        If localComp Is Nothing Then localComp = GetModLocalCompByKeywords("quilted_fabric_api", "quilted-fabric-api")
        If localComp Is Nothing Then Return Nothing
        Dim result = loaderOutput.FirstOrDefault(Function(comp) comp.Hash = localComp.ModrinthHash)
        If result IsNot Nothing Then
            _currentQsl = result
            _currentQslPath = localComp.Path
        End If
        Return result
    End Function

    '当前信息获取
    Public Sub GetCurrentInfo()
        SelectClear()
        BtnSelectStart.IsEnabled = True
        Dim CurrentVersion = PageVersionLeft.Version.Version
        SelectedMinecraftId = CurrentVersion.McName
        If CurrentVersion.HasLiteLoader Then
            SelectedLiteLoader = New DlLiteLoaderListEntry With {.Inherit = CurrentVersion.McName}
        End If
        If CurrentVersion.HasOptiFine Then
            SelectedOptiFine = New DlOptiFineListEntry With {.NameDisplay = CurrentVersion.McName + " " + CurrentVersion.OptiFineVersion, .IsPreview = CurrentVersion.OptiFineVersion.ContainsF("pre"), .Inherit = CurrentVersion.McName, .NameVersion = CurrentVersion.McName & "-OptiFine_HD_U_" & CurrentVersion.OptiFineVersion}
        End If
        If CurrentVersion.HasCleanroom Then
            SelectedAPIName = "Cleanroom"
            SelectedCleanroomVersion = CurrentVersion.CleanroomVersion
        ElseIf CurrentVersion.HasForge Then
            SelectedLoaderName = "Forge"
            SelectedForge = New DlForgeVersionEntry(CurrentVersion.ForgeVersion, Nothing, CurrentVersion.McName) With {.Category = "installer", .ForgeType = DlForgelikeEntry.ForgelikeType.Forge, .Inherit = CurrentVersion.McName}
        ElseIf CurrentVersion.HasFabric Then
            SelectedLoaderName = "Fabric"
            SelectedFabric = CurrentVersion.FabricVersion
            SelectedFabricApi = GetCurrentFabricApi() '检测已有 Fabric API
        ElseIf CurrentVersion.HasLabyMod Then
            SelectedLoaderName = "LabyMod"
            SelectedLabyModVersion = CurrentVersion.LabyModVersion
        ElseIf CurrentVersion.HasNeoForge Then
            SelectedLoaderName = "NeoForge"
            SelectedNeoForgeVersion = CurrentVersion.NeoForgeVersion
        ElseIf CurrentVersion.HasQuilt Then
            SelectedLoaderName = "Quilt"
            SelectedQuilt = CurrentVersion.QuiltVersion
            SelectedQSL = GetCurrentQsl() '检测已有 QSL
            SelectedFabricApi = GetCurrentFabricApi() '检测已有 Fabric API
        End If
        If (CurrentVersion.HasFabric OrElse CurrentVersion.HasQuilt) AndAlso CurrentVersion.HasOptiFine Then
            SelectedOptiFabric = Nothing 'TODO: 检测已有 OptiFabric
        End If
        SelectedMinecraftIcon = "pack://application:,,,/images/Blocks/Grass.png" 'TODO: 需要判断 Icon
        CurrentInfo = GetSelectInfo()
        EnterSelectPage()
    End Sub
    Private CurrentInfo As String = Nothing

#End Region

#Region "加载器"

    '结果数据化
    Private Sub LoadMinecraft_OnFinish()
        Try
            Dim Dict As New Dictionary(Of String, List(Of JObject)) From {
                {"正式版", New List(Of JObject)}, {"预览版", New List(Of JObject)}, {"远古版", New List(Of JObject)}, {"愚人节版", New List(Of JObject)}
            }
            Dim Versions As JArray = DlClientListLoader.Output.Value("versions")
            For Each Version As JObject In Versions
                '确定分类
                Dim Type As String = Version("type")
                Select Case Type
                    Case "release"
                        Type = "正式版"
                    Case "snapshot"
                        Type = "预览版"
                        'Mojang 误分类
                        If Version("id").ToString.StartsWith("1.") AndAlso
                            Not Version("id").ToString.ToLower.Contains("combat") AndAlso
                            Not Version("id").ToString.ToLower.Contains("rc") AndAlso
                            Not Version("id").ToString.ToLower.Contains("experimental") AndAlso
                            Not Version("id").ToString.ToLower.Contains("pre") Then
                            Type = "正式版"
                            Version("type") = "release"
                        End If
                        '愚人节版本
                        Select Case Version("id").ToString.ToLower
                            Case "20w14infinite", "20w14∞"
                                Type = "愚人节版"
                                Version("id") = "20w14∞"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                            Case "3d shareware v1.34", "1.rv-pre1", "15w14a", "2.0", "22w13oneblockatatime", "23w13a_or_b", "24w14potato"
                                Type = "愚人节版"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                            Case Else '4/1 自动视作愚人节版
                                Dim ReleaseDate = Version("releaseTime").Value(Of Date).ToUniversalTime().AddHours(2)
                                If ReleaseDate.Month = 4 AndAlso ReleaseDate.Day = 1 Then
                                    Type = "愚人节版"
                                    Version("type") = "special"
                                End If
                        End Select
                    Case "special"
                        '已被处理的愚人节版
                        Type = "愚人节版"
                    Case Else
                        Type = "远古版"
                End Select
                '加入辞典
                Dict(Type).Add(Version)
            Next
            '排序
            For i = 0 To Dict.Keys.Count - 1
                Dict(Dict.Keys(i)) = Sort(Dict.Values(i), Function(Left As JObject, Right As JObject) As Boolean
                                                              Return Left("releaseTime").Value(Of Date) > Right("releaseTime").Value(Of Date)
                                                          End Function)
            Next
            '清空当前
            PanMinecraft.Children.Clear()
            '添加最新版本
            Dim CardInfo As New MyCard With {.Title = "最新版本", .Margin = New Thickness(0, 15, 0, 15)}
            Dim TopestVersions As New List(Of JObject)
            Dim Release As JObject = Dict("正式版")(0).DeepClone()
            Release("lore") = "最新正式版，发布于 " & Release("releaseTime").Value(Of Date).ToString("yyyy'/'MM'/'dd HH':'mm")
            TopestVersions.Add(Release)
            If Dict("正式版")(0)("releaseTime").Value(Of Date) < Dict("预览版")(0)("releaseTime").Value(Of Date) Then
                Dim Snapshot As JObject = Dict("预览版")(0).DeepClone()
                Snapshot("lore") = "最新预览版，发布于 " & Snapshot("releaseTime").Value(Of Date).ToString("yyyy'/'MM'/'dd HH':'mm")
                TopestVersions.Add(Snapshot)
            End If
            Dim PanInfo As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = TopestVersions}
            Dim StackInstall = Sub(Stack As StackPanel)
                                   For Each item In Stack.Tag
                                       Stack.Children.Add(McDownloadListItem(item, Sub(sender, e) FrmVersionInstall.MinecraftSelected(sender, e), False))
                                   Next
                               End Sub
            MyCard.StackInstall(PanInfo, StackInstall)
            CardInfo.Children.Add(PanInfo)
            PanMinecraft.Children.Insert(0, CardInfo)
            '添加其他版本
            For Each Pair As KeyValuePair(Of String, List(Of JObject)) In Dict
                If Not Pair.Value.Any() Then Continue For
                '增加卡片
                Dim NewCard As New MyCard With {.Title = Pair.Key & " (" & Pair.Value.Count & ")", .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                '不能使用 AddressOf，这导致了 #535，原因完全不明，疑似是编译器 Bug
                NewCard.InstallMethod = StackInstall
                NewCard.IsSwaped = True
                PanMinecraft.Children.Add(NewCard)
            Next
            '自动选择版本
            If McVersionWaitingForSelect Is Nothing Then Exit Try
            Log("[Download] 自动选择 MC 版本：" & McVersionWaitingForSelect)
            For Each Version As JObject In Versions
                If Version("id").ToString <> McVersionWaitingForSelect Then Continue For
                Dim Item = McDownloadListItem(Version, Sub()
                                                       End Sub, False)
                MinecraftSelected(Item, Nothing)
            Next
        Catch ex As Exception
            Log(ex, "可视化安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub
    ''' <summary>
    ''' 当 MC 版本列表加载完时，立即自动选择的版本。用于外部调用。
    ''' </summary>
    Public Shared McVersionWaitingForSelect As String = Nothing

#End Region

#Region "OptiFine 列表"

    ''' <summary>
    ''' 获取 OptiFine 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadOptiFineGetError() As String
        If SelectedLoaderName = "NeoForge" OrElse SelectedLoaderName = "Quilt" Then Return $"与 {SelectedLoaderName} 不兼容"
        If LoadOptiFine Is Nothing OrElse LoadOptiFine.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadOptiFine.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadOptiFine.State, Object).Error.Message
        '检查 Forge 1.13 - 1.14.3：全部不兼容
        If SelectedLoaderName = "Forge" AndAlso
            VersionSortInteger(SelectedMinecraftId, "1.13") >= 0 AndAlso VersionSortInteger("1.14.3", SelectedMinecraftId) >= 0 Then
            Return "与 Forge 不兼容"
        End If
        '检查最低 Forge 版本
        Dim MinimalForgeVersion As String = "9999.9999"
        Dim NotSuitForForge As Boolean = False
        For Each OptiFineVersion As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
            If Not OptiFineVersion.NameDisplay.StartsWith(SelectedMinecraftId & " ") Then Continue For '不是同一个大版本
            If SelectedForge Is Nothing Then Return Nothing '该版本可用
            If IsOptiFineSuitForForge(OptiFineVersion, SelectedForge) Then
                Return Nothing '该版本可用
            Else
                NotSuitForForge = True
                If OptiFineVersion.RequiredForgeVersion IsNot Nothing Then
                    '设置用于显示的最低允许的 Forge 版本
                    MinimalForgeVersion = If(VersionSortBoolean(MinimalForgeVersion, OptiFineVersion.RequiredForgeVersion),
                                          OptiFineVersion.RequiredForgeVersion, MinimalForgeVersion)
                End If
            End If
        Next
        If MinimalForgeVersion = "9999.9999" Then
            Return If(NotSuitForForge, "与 Forge 不兼容", "没有可用版本")
        Else
            Return "需要 Forge " & If(MinimalForgeVersion.Contains("."), "", "#") & MinimalForgeVersion & " 或更高版本"
        End If
    End Function

    '检查某个 OptiFine 是否与某个 Forge 兼容（最低 Forge 版本是否达到需求）
    Private Function IsOptiFineSuitForForge(OptiFine As DlOptiFineListEntry, Forge As DlForgeVersionEntry)
        If Forge.Inherit <> OptiFine.Inherit Then Return False '不是同一个大版本
        If OptiFine.RequiredForgeVersion Is Nothing Then Return False '不兼容 Forge
        If String.IsNullOrWhiteSpace(OptiFine.RequiredForgeVersion) Then Return True '#4183
        If OptiFine.RequiredForgeVersion.Contains(".") Then 'XX.X.XXX
            Return VersionSortInteger(Forge.Version.ToString, OptiFine.RequiredForgeVersion) >= 0
        Else 'XXXX
            Return Forge.Version.Revision >= OptiFine.RequiredForgeVersion
        End If
    End Function

    '限制展开
    Private Sub CardOptiFine_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFine.PreviewSwap
        If LoadOptiFineGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 OptiFine 版本列表。
    ''' </summary>
    Private Sub OptiFine_Loaded() Handles LoadOptiFine.StateChanged
        Try
            If DlOptiFineListLoader.State <> LoadState.Finished Then Exit Sub

            '获取版本列表
            Dim Versions As New List(Of DlOptiFineListEntry)
            For Each Version As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
                If SelectedForge IsNot Nothing AndAlso Not IsOptiFineSuitForForge(Version, SelectedForge) Then Continue For
                If Version.NameDisplay.StartsWith(SelectedMinecraftId & " ") Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Exit Sub
            '排序
            Versions = Sort(Versions,
            Function(Left As DlOptiFineListEntry, Right As DlOptiFineListEntry) As Boolean
                If Not Left.IsPreview AndAlso Right.IsPreview Then Return True
                If Left.IsPreview AndAlso Not Right.IsPreview Then Return False
                Return VersionSortBoolean(Left.NameDisplay, Right.NameDisplay)
            End Function)
            '可视化
            PanOptiFine.Children.Clear()
            For Each Version In Versions
                PanOptiFine.Children.Add(OptiFineDownloadListItem(Version, AddressOf OptiFine_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 OptiFine 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub OptiFine_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFine = sender.Tag
        If SelectedForge IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, SelectedForge) Then SelectedForge = Nothing
        OptiFabric_Loaded()
        Forge_Loaded()
        NeoForge_Loaded()
        CardOptiFine.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub OptiFine_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnOptiFineClear.MouseLeftButtonUp
        SelectedOptiFine = Nothing
        SelectedOptiFabric = Nothing
        CardOptiFine.IsSwaped = True
        e.Handled = True
        Forge_Loaded()
        NeoForge_Loaded()
        SelectReload()
    End Sub

#End Region

#Region "LiteLoader 列表"

    ''' <summary>
    ''' 获取 LiteLoader 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadLiteLoaderGetError() As String
        If Not SelectedMinecraftId.Contains("1.") OrElse Val(SelectedMinecraftId.Split(".")(1)) > 12 Then Return "没有可用版本"
        If LoadLiteLoader Is Nothing OrElse LoadLiteLoader.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadLiteLoader.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadLiteLoader.State, Object).Error.Message
        For Each Version As DlLiteLoaderListEntry In DlLiteLoaderListLoader.Output.Value
            If Version.Inherit = SelectedMinecraftId Then Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardLiteLoader_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardLiteLoader.PreviewSwap
        If LoadLiteLoaderGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 LiteLoader 版本列表。
    ''' </summary>
    Private Sub LiteLoader_Loaded() Handles LoadLiteLoader.StateChanged
        Try
            If DlLiteLoaderListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Version As DlLiteLoaderListEntry In DlLiteLoaderListLoader.Output.Value
                If Version.Inherit = SelectedMinecraftId Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Exit Sub
            '可视化
            PanLiteLoader.Children.Clear()
            For Each Version In Versions
                PanLiteLoader.Children.Add(LiteLoaderDownloadListItem(Version, AddressOf LiteLoader_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 LiteLoader 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub LiteLoader_Selected(sender As MyListItem, e As EventArgs)
        SelectedLiteLoader = sender.Tag
        CardLiteLoader.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub LiteLoader_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnLiteLoaderClear.MouseLeftButtonUp
        SelectedLiteLoader = Nothing
        CardLiteLoader.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Forge 列表"

    ''' <summary>
    ''' 获取 Forge 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadForgeGetError() As String
        If Not SelectedMinecraftId.StartsWith("1.") Then Return "没有可用版本"
        If Not LoadForge.State.IsLoader Then Return "正在获取版本列表……"
        Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
        If SelectedMinecraftId <> Loader.Input Then Return "正在获取版本列表……"
        If Loader.State = LoadState.Loading Then Return "正在获取版本列表……"
        If Loader.State = LoadState.Failed Then
            Dim ErrorMessage As String = Loader.Error.Message
            If ErrorMessage.Contains("没有可用版本") Then
                Return "没有可用版本"
            Else
                Return "获取版本列表失败：" & ErrorMessage
            End If
        End If
        If Loader.State <> LoadState.Finished Then Return "获取版本列表失败：未知错误，状态为 " & GetStringFromEnum(Loader.State)
        Dim NotSuitForOptiFine As Boolean = False
        For Each Version In Loader.Output
            If Version.Category = "universal" OrElse Version.Category = "client" Then Continue For '跳过无法自动安装的版本
            If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "Forge" Then Return $"与 {SelectedLoaderName} 不兼容"
            If SelectedOptiFine IsNot Nothing AndAlso
                VersionSortInteger(SelectedMinecraftId, "1.13") >= 0 AndAlso VersionSortInteger("1.14.3", SelectedMinecraftId) >= 0 Then
                Return "与 OptiFine 不兼容" '1.13 ~ 1.14.3 OptiFine 检查
            End If
            If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, Version) Then
                NotSuitForOptiFine = True '与 OptiFine 不兼容
                Continue For
            End If
            Return Nothing
        Next
        Return If(NotSuitForOptiFine, "与 OptiFine 不兼容", "该版本不支持自动安装")
    End Function

    '限制展开
    Private Sub CardForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardForge.PreviewSwap
        If LoadForgeGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Forge 版本列表。
    ''' </summary>
    Private Sub Forge_Loaded() Handles LoadForge.StateChanged
        Try
            If Not LoadForge.State.IsLoader Then Exit Sub
            Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
            If SelectedMinecraftId <> Loader.Input Then Exit Sub
            If Loader.State <> LoadState.Finished Then Exit Sub
            '获取要显示的版本
            Dim Versions = Loader.Output.ToList '复制数组，以免 Output 在实例化后变空
            If Not Loader.Output.Any() Then Exit Sub
            PanForge.Children.Clear()
            Versions = Sort(Versions, Function(a, b) a.Version > b.Version).Where(
            Function(v)
                If v.Category = "universal" OrElse v.Category = "client" Then Return False '跳过无法自动安装的版本
                If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, v) Then Return False
                Return True
            End Function).ToList()
            ForgeDownloadListItemPreload(PanForge, Versions, AddressOf Forge_Selected, False)
            For Each Version In Versions
                PanForge.Children.Add(ForgeDownloadListItem(Version, AddressOf Forge_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Forge 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub Forge_Selected(sender As MyListItem, e As EventArgs)
        SelectedForge = sender.Tag
        SelectedLoaderName = "Forge"
        CardForge.IsSwaped = True
        If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, SelectedForge) Then SelectedOptiFine = Nothing
        OptiFine_Loaded()
        SelectReload()
    End Sub
    Private Sub Forge_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnForgeClear.MouseLeftButtonUp
        SelectedForge = Nothing
        SelectedLoaderName = Nothing
        CardForge.IsSwaped = True
        e.Handled = True
        OptiFine_Loaded()
        SelectReload()
    End Sub

#End Region

#Region "NeoForge 列表"

    ''' <summary>
    ''' 获取 NeoForge 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadNeoForgeGetError() As String
        If Not SelectedMinecraftId.StartsWith("1.") Then Return "没有可用版本"
        If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
        If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "NeoForge" Then Return $"与 {SelectedLoaderName} 不兼容"
        If LoadNeoForge Is Nothing OrElse LoadNeoForge.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadNeoForge.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadNeoForge.State, Object).Error.Message
        If DlNeoForgeListLoader.Output.Value.Any(Function(v) v.Inherit = SelectedMinecraftId) Then
            Return Nothing
        Else
            Return "没有可用版本"
        End If
    End Function

    '限制展开
    Private Sub CardNeoForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardNeoForge.PreviewSwap
        If LoadNeoForgeGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 NeoForge 版本列表。
    ''' </summary>
    Private Sub NeoForge_Loaded() Handles LoadNeoForge.StateChanged
        Try
            '获取版本列表
            If DlNeoForgeListLoader.State <> LoadState.Finished Then Exit Sub
            Dim Versions = DlNeoForgeListLoader.Output.Value.Where(Function(v) v.Inherit = SelectedMinecraftId).ToList
            If Not Versions.Any() Then Exit Sub
            '可视化
            PanNeoForge.Children.Clear()
            NeoForgeDownloadListItemPreload(PanNeoForge, Versions, AddressOf NeoForge_Selected, False)
            For Each Version In Versions
                PanNeoForge.Children.Add(NeoForgeDownloadListItem(Version, AddressOf NeoForge_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 NeoForge 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub NeoForge_Selected(sender As MyListItem, e As EventArgs)
        SelectedNeoForge = sender.Tag
        SelectedNeoForgeVersion = Nothing
        SelectedLoaderName = "NeoForge"
        CardNeoForge.IsSwaped = True
        OptiFine_Loaded()
        SelectReload()
    End Sub
    Private Sub NeoForge_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnNeoForgeClear.MouseLeftButtonUp
        SelectedNeoForge = Nothing
        SelectedNeoForgeVersion = Nothing
        SelectedLoaderName = Nothing
        CardNeoForge.IsSwaped = True
        e.Handled = True
        OptiFine_Loaded()
        SelectReload()
    End Sub

#End Region

#Region "Cleanroom 列表"

    ''' <summary>
    ''' 获取 Cleanroom 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadCleanroomGetError() As String
        If Not SelectedMinecraftId.StartsWith("1.") Then Return "没有可用版本"
        If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
        If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "Cleanroom" Then Return $"与 {SelectedLoaderName} 不兼容"
        If LoadCleanroom Is Nothing OrElse LoadCleanroom.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadCleanroom.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadCleanroom.State, Object).Error.Message
        Return Nothing
        'If DlCleanroomListLoader.Output.Value.Any(Function(v) v.Inherit = SelectedMinecraftId) Then
        '    Return Nothing
        'Else
        '    Return "没有可用版本"
        'End If
    End Function

    '限制展开
    Private Sub CardCleanroom_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardCleanroom.PreviewSwap
        If LoadCleanroomGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Cleanroom 版本列表。
    ''' </summary>
    Private Sub Cleanroom_Loaded() Handles LoadCleanroom.StateChanged
        Try
            '获取版本列表
            If DlCleanroomListLoader.State <> LoadState.Finished Then Exit Sub
            Dim Versions = DlCleanroomListLoader.Output.Value.Where(Function(v) v.Inherit = SelectedMinecraftId).ToList
            If Not Versions.Any() Then Exit Sub
            '可视化
            PanCleanroom.Children.Clear()
            CleanroomDownloadListItemPreload(PanCleanroom, Versions, AddressOf Cleanroom_Selected, False)
            For Each Version In Versions
                PanCleanroom.Children.Add(CleanroomDownloadListItem(Version, AddressOf Cleanroom_Selected, False))
            Next
        Catch ex As Exception
            Log(ex, "可视化 Cleanroom 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub Cleanroom_Selected(sender As MyListItem, e As EventArgs)
        SelectedCleanroom = sender.Tag
        SelectedCleanroomVersion = Nothing
        SelectedLoaderName = "Cleanroom"
        CardCleanroom.IsSwaped = True
        OptiFine_Loaded()
        SelectReload()
    End Sub
    Private Sub Cleanroom_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnCleanroomClear.MouseLeftButtonUp
        SelectedCleanroom = Nothing
        SelectedCleanroomVersion = Nothing
        SelectedLoaderName = Nothing
        CardCleanroom.IsSwaped = True
        e.Handled = True
        OptiFine_Loaded()
        SelectReload()
    End Sub

#End Region

#Region "Fabric 列表"

    ''' <summary>
    ''' 获取 Fabric 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadFabricGetError() As String
        If LoadFabric Is Nothing OrElse LoadFabric.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadFabric.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadFabric.State, Object).Error.Message
        For Each Version As JObject In DlFabricListLoader.Output.Value("game")
            If Version("version").ToString = SelectedMinecraftId.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3") Then
                If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "Fabric" Then Return $"与 {SelectedLoaderName} 不兼容"
                Return Nothing
            End If
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabric.PreviewSwap
        If LoadFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Fabric 版本列表。
    ''' </summary>
    Private Sub Fabric_Loaded() Handles LoadFabric.StateChanged
        Try
            If DlFabricListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As JArray = DlFabricListLoader.Output.Value("loader")
            If Not Versions.Any() Then Exit Sub
            '可视化
            PanFabric.Children.Clear()
            PanFabric.Tag = Versions
            CardFabric.SwapControl = PanFabric
            CardFabric.InstallMethod = Sub(Stack As StackPanel)
                                           For Each item In Stack.Tag
                                               Stack.Children.Add(FabricDownloadListItem(CType(item, JObject), AddressOf FrmVersionInstall.Fabric_Selected))
                                           Next
                                       End Sub
        Catch ex As Exception
            Log(ex, "可视化 Fabric 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Public Sub Fabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabric = sender.Tag("version").ToString
        SelectedLoaderName = "Fabric"
        FabricApi_Loaded()
        OptiFabric_Loaded()
        CardFabric.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub Fabric_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnFabricClear.MouseLeftButtonUp
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        SelectedOptiFabric = Nothing
        SelectedLoaderName = Nothing
        SelectedAPIName = Nothing
        CardFabric.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Fabric API 列表"

    ''' <summary>
    ''' 从显示名判断该 API 是否与某版本适配。
    ''' </summary>
    Public Shared Function IsSuitableFabricApi(DisplayName As String, MinecraftVersion As String) As Boolean
        Try
            If DisplayName Is Nothing OrElse MinecraftVersion Is Nothing Then Return False
            DisplayName = DisplayName.ToLower : MinecraftVersion = MinecraftVersion.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3").ToLower
            If DisplayName.StartsWith("[" & MinecraftVersion & "]") Then Return True
            If Not DisplayName.Contains("/") OrElse Not DisplayName.Contains("]") Then Return False
            '直接的判断（例如 1.18.1/22w03a）
            For Each Part As String In DisplayName.BeforeFirst("]").TrimStart("[").Split("/")
                If Part = MinecraftVersion Then Return True
            Next
            '将版本名分割语素（例如 1.16.4/5）
            Dim Lefts = RegexSearch(DisplayName.BeforeFirst("]"), "[a-z/]+|[0-9/]+")
            Dim Rights = RegexSearch(MinecraftVersion.BeforeFirst("]"), "[a-z/]+|[0-9/]+")
            '对每段进行判断
            Dim i As Integer = 0
            While True
                '两边均缺失，感觉是一个东西
                If Lefts.Count - 1 < i AndAlso Rights.Count - 1 < i Then Return True
                '确定两边是否一致
                Dim LeftValue As String = If(Lefts.Count - 1 < i, "-1", Lefts(i))
                Dim RightValue As String = If(Rights.Count - 1 < i, "-1", Rights(i))
                If Not LeftValue.Contains("/") Then
                    If LeftValue <> RightValue Then Return False
                Else
                    '左边存在斜杠
                    If Not LeftValue.Contains(RightValue) Then Return False
                End If
                i += 1
            End While
            Return True
        Catch ex As Exception
            Log(ex, "判断 Fabric API 版本适配性出错（" & DisplayName & ", " & MinecraftVersion & "）")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取 FabricApi 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadFabricApiGetError() As String
        If LoadFabricApi Is Nothing OrElse LoadFabricApi.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadFabricApi.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadFabricApi.State, Object).Error.Message
        If SelectedAPIName IsNot Nothing AndAlso SelectedAPIName IsNot "Fabric API" Then Return $"与 {SelectedAPIName} 不兼容"
        If DlFabricApiLoader.Output Is Nothing Then
            If SelectedFabric Is Nothing AndAlso SelectedQuilt Is Nothing Then Return "需要安装 Fabric / Quilt"
            Return "正在获取版本列表……"
        End If
        For Each Version In DlFabricApiLoader.Output
            If Not IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then Continue For
            If SelectedFabric Is Nothing AndAlso SelectedQuilt Is Nothing Then Return "需要安装 Fabric / Quilt"
            Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardFabricApi_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabricApi.PreviewSwap
        If LoadFabricApiGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private AutoSelectedFabricApi As Boolean = False
    ''' <summary>
    ''' 尝试重新可视化 FabricApi 版本列表。
    ''' </summary>
    Private Sub FabricApi_Loaded() Handles LoadFabricApi.StateChanged
        Try
            If DlFabricApiLoader.State <> LoadState.Finished Then Exit Sub
            If SelectedMinecraftId Is Nothing OrElse (SelectedFabric Is Nothing AndAlso SelectedQuilt Is Nothing) Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of CompFile)
            For Each Version In DlFabricApiLoader.Output
                If IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then
                    If Not Version.DisplayName.StartsWith("[") Then
                        Log("[Download] 已特判修改 Fabric API 显示名：" & Version.DisplayName, LogLevel.Debug)
                        Version.DisplayName = "[" & SelectedMinecraftId & "] " & Version.DisplayName
                    End If
                    Versions.Add(Version)
                End If
            Next
            If Not Versions.Any() Then Exit Sub
            Versions = Sort(Versions, Function(a, b) a.ReleaseDate > b.ReleaseDate)
            '可视化
            PanFabricApi.Children.Clear()
            For Each Version In Versions
                If Not IsSuitableFabricApi(Version.DisplayName, SelectedMinecraftId) Then Continue For
                PanFabricApi.Children.Add(FabricApiDownloadListItem(Version, AddressOf FabricApi_Selected))
            Next
            '检测已经存在的 Fabric API
            Dim currentInstalled = GetCurrentFabricApi()
            If currentInstalled IsNot Nothing Then
                SelectedFabricApi = currentInstalled
                SelectedAPIName = "Fabric API"
                SelectReload()
            '自动选择 Fabric API
            ElseIf (Not AutoSelectedFabricApi AndAlso SelectedQuilt Is Nothing) OrElse (SelectedQuilt IsNot Nothing AndAlso LoadQSLGetError() Is "没有可用版本") Then
                AutoSelectedFabricApi = True
                Log($"[Download] 已自动选择 Fabric API：{CType(PanFabricApi.Children(0), MyListItem).Title}")
                FabricApi_Selected(PanFabricApi.Children(0), Nothing)
            End If
        Catch ex As Exception
            Log(ex, "可视化 Fabric API 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub FabricApi_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabricApi = sender.Tag
        SelectedAPIName = "Fabric API"
        CardFabricApi.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub FabricApi_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnFabricApiClear.MouseLeftButtonUp
        SelectedFabricApi = _currentFabricApi
        SelectedAPIName = If(SelectedFabricApi Is Nothing, Nothing, "Fabric API")
        CardFabricApi.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "Quilt 列表"

    ''' <summary>
    ''' 获取 Quilt 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadQuiltGetError() As String
        If LoadQuilt Is Nothing OrElse LoadQuilt.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadQuilt.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadQuilt.State, Object).Error.Message
        For Each Version As JObject In DlQuiltListLoader.Output.Value("game")
            If Version("version").ToString = SelectedMinecraftId.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3") Then
                If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
                If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "Quilt" Then Return $"与 {SelectedLoaderName} 不兼容"
                Return Nothing
            End If
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardQuilt_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardQuilt.PreviewSwap
        If LoadQuiltGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 Quilt 版本列表。
    ''' </summary>
    Private Sub Quilt_Loaded() Handles LoadQuilt.StateChanged
        Try
            If DlQuiltListLoader.State <> LoadState.Finished Then Exit Sub
            '获取版本列表
            Dim Versions As JArray = DlQuiltListLoader.Output.Value("loader")
            If Not Versions.Any() Then Exit Sub
            '可视化
            PanQuilt.Children.Clear()
            PanQuilt.Tag = Versions
            CardQuilt.SwapControl = PanQuilt
            CardQuilt.InstallMethod = Sub(Stack As StackPanel)
                                          For Each item In Stack.Tag
                                              Stack.Children.Add(QuiltDownloadListItem(CType(item, JObject), AddressOf FrmVersionInstall.Quilt_Selected))
                                          Next
                                      End Sub
        Catch ex As Exception
            Log(ex, "可视化 Quilt 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Public Sub Quilt_Selected(sender As MyListItem, e As EventArgs)
        SelectedQuilt = sender.Tag("version").ToString
        SelectedLoaderName = "Quilt"
        FabricApi_Loaded()
        QSL_Loaded()
        CardQuilt.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub Quilt_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnQuiltClear.MouseLeftButtonUp
        SelectedQuilt = Nothing
        SelectedQSL = Nothing
        SelectedFabricApi = Nothing
        SelectedLoaderName = Nothing
        SelectedAPIName = Nothing
        CardQuilt.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "QSL 列表"

    ''' <summary>
    ''' 从显示名判断该 API 是否与某版本适配。
    ''' </summary>
    Public Shared Function IsSuitableQSL(SupportVersions As List(Of String), MinecraftVersion As String) As Boolean
        Try
            If SupportVersions.Contains(MinecraftVersion) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Log(ex, "判断 QSL 版本适配性出错（" & SupportVersions.ToString & ", " & MinecraftVersion & "）")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取 QSL 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadQSLGetError() As String
        If LoadQSL Is Nothing OrElse LoadQSL.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadQSL.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadQSL.State, Object).Error.Message
        If SelectedAPIName IsNot Nothing AndAlso SelectedAPIName IsNot "QFAPI / QSL" Then Return $"与 {SelectedAPIName} 不兼容"
        If DlQSLLoader.Output Is Nothing Then
            If SelectedQuilt Is Nothing Then Return "需要安装 Quilt"
            Return "正在获取版本列表……"
        End If
        For Each Version In DlQSLLoader.Output
            If Not IsSuitableQSL(Version.GameVersions, SelectedMinecraftId) Then Continue For
            If SelectedQuilt Is Nothing Then Return "需要安装 Quilt"
            Return Nothing
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardQSL_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardQSL.PreviewSwap
        If LoadQSLGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private AutoSelectedQSL As Boolean = False
    ''' <summary>
    ''' 尝试重新可视化 QSL 版本列表。
    ''' </summary>
    Private Sub QSL_Loaded() Handles LoadQSL.StateChanged
        Try
            If DlQSLLoader.State <> LoadState.Finished Then Exit Sub
            If SelectedMinecraftId Is Nothing OrElse SelectedQuilt Is Nothing Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of CompFile)
            For Each Version In DlQSLLoader.Output
                If IsSuitableQSL(Version.GameVersions, SelectedMinecraftId) Then
                    If Not Version.DisplayName.StartsWith("[") Then
                        Log("[Download] 已特判修改 QSL 显示名：" & Version.DisplayName, LogLevel.Debug)
                        Version.DisplayName = "[" & SelectedMinecraftId & "] " & Version.DisplayName
                    End If
                    Versions.Add(Version)
                End If
            Next
            If Not Versions.Any() Then Exit Sub
            Versions = Sort(Versions, Function(a, b) a.ReleaseDate > b.ReleaseDate)
            '可视化
            PanQSL.Children.Clear()
            For Each Version In Versions
                If Not IsSuitableQSL(Version.GameVersions, SelectedMinecraftId) Then Continue For
                PanQSL.Children.Add(QSLDownloadListItem(Version, AddressOf QSL_Selected))
            Next
            '检测已经存在的 QSL
            Dim currentInstalled = GetCurrentQsl()
            If currentInstalled IsNot Nothing Then
                SelectedQSL = currentInstalled
                SelectedAPIName = "QFAPI / QSL"
                SelectReload()
            '自动选择 QSL
            ElseIf Not AutoSelectedQSL Then
                AutoSelectedQSL = True
                Log($"[Download] 已自动选择 QSL：{CType(PanQSL.Children(0), MyListItem).Title}")
                QSL_Selected(PanQSL.Children(0), Nothing)
            End If
        Catch ex As Exception
            Log(ex, "可视化 QSL 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub QSL_Selected(sender As MyListItem, e As EventArgs)
        SelectedQSL = sender.Tag
        SelectedAPIName = "QFAPI / QSL"
        CardQSL.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub QSL_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnQSLClear.MouseLeftButtonUp
        SelectedQSL = _currentQsl
        SelectedAPIName = If(SelectedQSL Is Nothing, Nothing, "QFAPI / QSL")
        CardQSL.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "LabyMod 列表"

    ''' <summary>
    ''' 获取 LabyMod 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadLabyModGetError() As String
        If LoadLabyMod Is Nothing OrElse LoadLabyMod.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "加载中……"
        If LoadLabyMod.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadLabyMod.State, Object).Error.Message
        For Each Version As JObject In DlLabyModListLoader.Output.Value("production")("minecraftVersions")
            If Version("version").ToString = SelectedMinecraftId Then
                If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
                If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "LabyMod" Then Return $"与 {SelectedLoaderName} 不兼容"
                Return Nothing
            End If
        Next
        For Each Version As JObject In DlLabyModListLoader.Output.Value("snapshot")("minecraftVersions")
            If Version("version").ToString = SelectedMinecraftId Then
                If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
                If SelectedLoaderName IsNot Nothing AndAlso SelectedLoaderName IsNot "LabyMod" Then Return $"与 {SelectedLoaderName} 不兼容"
                Return Nothing
            End If
        Next
        Return "不可用"
    End Function

    '限制展开
    Private Sub CardLabyMod_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardLabyMod.PreviewSwap
        If LoadLabyModGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 LabyMod 版本列表。
    ''' </summary>
    Private Sub LabyMod_Loaded() Handles LoadLabyMod.StateChanged
        Try
            If LoadLabyMod.State.LoadingState = MyLoading.MyLoadingState.Run Then Exit Sub
            '获取版本列表
            Dim Versions As JObject = DlLabyModListLoader.Output.Value
            If Versions("production") Is Nothing OrElse Versions("snapshot") Is Nothing Then Exit Sub
            '可视化
            Dim ProcessedVersions As New JArray
            For Each Production As JObject In Versions("production")("minecraftVersions")
                If Production("version").ToString = SelectedMinecraftId Then
                    Dim ProductionVersion As New JObject
                    ProductionVersion.Add("version", Versions("production")("labyModVersion"))
                    ProductionVersion.Add("channel", "production")
                    ProductionVersion.Add("commitReference", Versions("production")("commitReference"))
                    ProcessedVersions.Add(ProductionVersion)
                End If
            Next
            For Each Snapshot As JObject In Versions("snapshot")("minecraftVersions")
                If Snapshot("version").ToString = SelectedMinecraftId Then
                    Dim SnapshotVersion As New JObject
                    SnapshotVersion.Add("version", Versions("production")("labyModVersion"))
                    SnapshotVersion.Add("channel", "snapshot")
                    SnapshotVersion.Add("commitReference", Versions("snapshot")("commitReference"))
                    ProcessedVersions.Add(SnapshotVersion)
                End If
            Next
            'MyMsgBox(If(ProcessedVersions.ToString, "Nothing"))
            PanLabyMod.Children.Clear()
            PanLabyMod.Tag = ProcessedVersions
            CardLabyMod.SwapControl = PanLabyMod
            CardLabyMod.InstallMethod = Sub(Stack As StackPanel)
                                            For Each item As JObject In Stack.Tag
                                                Stack.Children.Add(LabyModDownloadListItem(item, AddressOf LabyMod_Selected))
                                            Next
                                        End Sub
        Catch ex As Exception
            Log(ex, "可视化 LabyMod 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Public Sub LabyMod_Selected(sender As MyListItem, e As EventArgs)
        SelectedLabyModChannel = sender.Tag("channel").ToString
        SelectedLabyModCommitRef = sender.Tag("commitReference").ToString
        SelectedLabyModVersion = sender.Tag("version").ToString & If(SelectedLabyModChannel = "snapshot", " 快照版", " 稳定版")
        SelectedLoaderName = "LabyMod"
        CardLabyMod.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub LabyMod_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnLabyModClear.MouseLeftButtonUp
        SelectedLabyModCommitRef = Nothing
        SelectedLabyModVersion = Nothing
        SelectedLabyModChannel = Nothing
        SelectedLoaderName = Nothing
        SelectedAPIName = Nothing
        CardLabyMod.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub
#End Region

#Region "OptiFabric 列表"

    ''' <summary>
    ''' 从显示名判断该 Mod 是否与某版本适配。
    ''' </summary>
    Private Function IsSuitableOptiFabric(ModFile As CompFile, MinecraftVersion As String) As Boolean
        Try
            If MinecraftVersion Is Nothing Then Return False
            Return ModFile.GameVersions.Contains(MinecraftVersion)
        Catch ex As Exception
            Log(ex, "判断 OptiFabric 版本适配性出错（" & MinecraftVersion & "）")
            Return False
        End Try
    End Function

    Private AutoSelectedOptiFabric As Boolean = False
    ''' <summary>
    ''' 获取 OptiFabric 的加载异常信息。若正常则返回 Nothing。
    ''' </summary>
    Private Function LoadOptiFabricGetError() As String
        If SelectedMinecraftId.StartsWith("1.14") OrElse SelectedMinecraftId.StartsWith("1.15") Then Return "不兼容老版本 Fabric，请手动下载 OptiFabric Origins"
        If LoadOptiFabric Is Nothing OrElse LoadOptiFabric.State.LoadingState = MyLoading.MyLoadingState.Run Then Return "正在获取版本列表……"
        If LoadOptiFabric.State.LoadingState = MyLoading.MyLoadingState.Error Then Return "获取版本列表失败：" & CType(LoadOptiFabric.State, Object).Error.Message
        If DlOptiFabricLoader.Output Is Nothing Then
            If SelectedFabric Is Nothing AndAlso SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine 与 Fabric"
            If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
            If SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine"
            Return "正在获取版本列表……"
        End If
        For Each Version In DlOptiFabricLoader.Output
            If Not IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Continue For '2135#
            If SelectedFabric Is Nothing AndAlso SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine 与 Fabric"
            If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
            If SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine"
            Return Nothing '通过检查
        Next
        Return "没有可用版本"
    End Function

    '限制展开
    Private Sub CardOptiFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFabric.PreviewSwap
        If LoadOptiFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    ''' <summary>
    ''' 尝试重新可视化 OptiFabric 版本列表。
    ''' </summary>
    Private Sub OptiFabric_Loaded() Handles LoadOptiFabric.StateChanged
        Try
            If DlOptiFabricLoader.State <> LoadState.Finished Then Exit Sub
            If SelectedMinecraftId Is Nothing OrElse SelectedFabric Is Nothing OrElse SelectedOptiFine Is Nothing Then Exit Sub
            '获取版本列表
            Dim Versions As New List(Of CompFile)
            For Each Version In DlOptiFabricLoader.Output
                If IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Exit Sub
            '排序
            Versions = Sort(Versions, Function(a, b) a.ReleaseDate > b.ReleaseDate)
            '可视化
            PanOptiFabric.Children.Clear()
            For Each Version In Versions
                If Not IsSuitableOptiFabric(Version, SelectedMinecraftId) Then Continue For
                PanOptiFabric.Children.Add(OptiFabricDownloadListItem(Version, AddressOf OptiFabric_Selected))
            Next
            '自动选择 OptiFabric
            If Not AutoSelectedOptiFabric AndAlso
                Not (SelectedMinecraftId.StartsWith("1.14") OrElse SelectedMinecraftId.StartsWith("1.15")) Then '1.14~15 不自动选择
                AutoSelectedOptiFabric = True
                Log($"[Download] 已自动选择 OptiFabric：{CType(PanOptiFabric.Children(0), MyListItem).Title}")
                OptiFabric_Selected(PanOptiFabric.Children(0), Nothing)
            End If
        Catch ex As Exception
            Log(ex, "可视化 OptiFabric 安装版本列表出错", LogLevel.Feedback)
        End Try
    End Sub

    '选择与清除
    Private Sub OptiFabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFabric = sender.Tag
        CardOptiFabric.IsSwaped = True
        SelectReload()
    End Sub
    Private Sub OptiFabric_Clear(sender As Object, e As MouseButtonEventArgs) Handles BtnOptiFabricClear.MouseLeftButtonUp
        SelectedOptiFabric = Nothing
        CardOptiFabric.IsSwaped = True
        e.Handled = True
        SelectReload()
    End Sub

#End Region

#Region "安装"

    Private Sub BtnSelectStart_Click() Handles BtnSelectStart.Click
        '确认版本隔离
        If SelectedLoaderName IsNot Nothing AndAlso
           (Setup.Get("LaunchArgumentIndieV2") = 0 OrElse Setup.Get("LaunchArgumentIndieV2") = 2) Then
            If MyMsgBox("你尚未开启版本隔离，这会导致多个 MC 共用同一个 Mod 文件夹。" & vbCrLf &
                        "因此在切换 MC 版本时，MC 会因为读取到与当前版本不符的 Mod 而崩溃。" & vbCrLf &
                        "PCL 推荐你在开始下载前，在 设置 → 版本隔离 中开启版本隔离选项！", "版本隔离提示", "取消下载", "继续") = 1 Then
                Exit Sub
            End If
        End If
        If BtnSelectStart.Text = "开始重置" Then
            If MyMsgBox("你正在重置当前版本。" & vbCrLf &
                        "PCL 将会重新联网下载该版本所需的文件，并重新安装 Mod 加载器（如有）。" & vbCrLf &
                        "此操作不会丢失你的存档、Mod、资源包等。", "重置此版本", "继续", "取消") = 2 Then
                Exit Sub
            End If
        End If
        '删除 LabyMod Neo 文件
        If PageVersionLeft.Version.PathIndie <> PageVersionLeft.Version.Path AndAlso PageVersionLeft.Version.Version.HasLabyMod Then
            Directory.Delete(PageVersionLeft.Version.PathIndie & "labymod-neo", True)
        End If
        '备份版本核心文件
        CopyFile(PageVersionLeft.Version.Path + PageVersionLeft.Version.Name + ".json", PageVersionLeft.Version.Path + "PCLInstallBackups\" + PageVersionLeft.Version.Name + ".json")
        If File.Exists(PageVersionLeft.Version.Path + PageVersionLeft.Version.Name + ".jar") Then
            CopyFile(PageVersionLeft.Version.Path + PageVersionLeft.Version.Name + ".jar", PageVersionLeft.Version.Path + "PCLInstallBackups\" + PageVersionLeft.Version.Name + ".jar")
        End If
        '确认独立 API (如 Fabric API 等) 是否需要被修改
        If SelectedFabricApi?.Equals(_currentFabricApi) Then SelectedFabricApi = Nothing
        If SelectedQSL?.Equals(_currentQsl) Then SelectedQSL = Nothing
        '提交安装申请
        Dim Request As New McInstallRequest With {
            .TargetVersionName = PageVersionLeft.Version.Name,
            .TargetVersionFolder = $"{PathMcFolder}versions\{PageVersionLeft.Version.Name}\",
            .MinecraftJson = SelectedMinecraftJsonUrl,
            .MinecraftName = SelectedMinecraftId,
            .OptiFineEntry = SelectedOptiFine,
            .ForgeEntry = SelectedForge,
            .NeoForgeEntry = SelectedNeoForge,
            .NeoForgeVersion = SelectedNeoForgeVersion,
            .CleanroomEntry = SelectedCleanroom,
            .CleanroomVersion = SelectedCleanroomVersion,
            .FabricVersion = SelectedFabric,
            .FabricApi = SelectedFabricApi,
            .QuiltVersion = SelectedQuilt,
            .QSL = SelectedQSL,
            .OptiFabric = SelectedOptiFabric,
            .LiteLoaderEntry = SelectedLiteLoader,
            .LabyModChannel = SelectedLabyModChannel,
            .LabyModCommitRef = SelectedLabyModCommitRef
        }
        BtnSelectStart.IsEnabled = False
        If Not McInstall(Request, BtnSelectStart.Text.AfterFirst("开始")) Then Exit Sub
        '删除旧的独立 API 文件
        If SelectedFabricApi IsNot Nothing And _currentFabricApiPath IsNot Nothing Then File.Delete(_currentFabricApiPath)
        If SelectedQSL IsNot Nothing And _currentQslPath IsNot Nothing Then File.Delete(_currentQslPath)
        '返回主页
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.Launch})
    End Sub

#End Region

End Class
