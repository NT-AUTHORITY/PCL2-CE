Public Class PageVersionCompResource
    Implements IRefreshable
#Region "初始化"

    Private CurrentCompType As CompType = CompType.Mod

    Private CurrentSwipSelect As MyLocalCompItem.SwipeSelect

    Public Sub New(LoadCompType As CompType)
        CurrentCompType = LoadCompType
        CurrentFolderPath = "" '确保文件夹路径被重置为根目录
        CurrentSwipSelect = New MyLocalCompItem.SwipeSelect() With {.TargetFrm = Me}

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。

        If {CompType.Shader, CompType.ResourcePack, CompType.Schematic}.Contains(CurrentCompType) Then
            BtnSelectEnable.Visibility = Visibility.Collapsed
            BtnSelectDisable.Visibility = Visibility.Collapsed
        End If
        
        '投影文件管理页隐藏下载按钮
        If CurrentCompType = CompType.Schematic Then
            BtnManageDownload.Visibility = Visibility.Collapsed
            BtnHintDownload.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Function GetRequireLoaderData() As CompLocalLoaderData
        Dim res As New CompLocalLoaderData
        res.GameVersion = PageVersionLeft.Version
        res.Frm = Me
        Dim RequireLoaders As New List(Of CompLoaderType)
        Select Case CurrentCompType
            Case CompType.Mod
                RequireLoaders = GetCurrentVersionModLoader()
            Case CompType.ResourcePack
                RequireLoaders = {CompLoaderType.Minecraft}.ToList()
            Case CompType.Shader
                RequireLoaders = {CompLoaderType.OptiFine, CompLoaderType.Iris, CompLoaderType.Vanilla, CompLoaderType.Canvas}.ToList()
            Case CompType.Schematic
                RequireLoaders = {CompLoaderType.Minecraft}.ToList()
        End Select
        res.Loaders = RequireLoaders
        res.CompPath = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
        res.CompType = CurrentCompType
        Return res
    End Function

    Private IsLoad As Boolean = False
    Public Sub PageOther_Loaded() Handles Me.Loaded

        If FrmMain.PageLast.Page <> FormMain.PageType.CompDetail Then PanBack.ScrollToHome()
        AniControlEnabled += 1
        SelectedMods.Clear()
        ReloadCompFileList()
        ChangeAllSelected(False)
        AniControlEnabled -= 1

        '非重复加载部分
        If IsLoad Then Return
        IsLoad = True

        AddHandler FrmMain.KeyDown, AddressOf FrmMain_KeyDown
        '调整按钮边距（这玩意儿没法从 XAML 改）
        For Each Btn As MyRadioButton In PanFilter.Children
            Btn.LabText.Margin = New Thickness(-2, 0, 8, 0)
        Next

    End Sub
    ''' <summary>
    ''' 刷新 Mod 列表。
    ''' </summary>
    Public Sub ReloadCompFileList(Optional ForceReload As Boolean = False)
        If LoaderRun(If(ForceReload, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated)) Then
            Log($"[System] 已刷新 {CurrentCompType} 列表")
            RunInUi(Sub()
                        Filter = FilterType.All
                        PanBack.ScrollToHome()
                        SearchBox.Text = ""
                    End Sub)
        End If
    End Sub
    '强制刷新
    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh(CurrentCompType)
    End Sub
    Public Shared Sub Refresh(WhichPage As CompType)
        '强制刷新
        Try
            CompProjectCache.Clear()
            CompFilesCache.Clear()
            File.Delete(PathTemp & "Cache\LocalComp.json")
            Log("[CompResource] 由于点击刷新按钮，清理本地工程信息缓存")
        Catch ex As Exception
            Log(ex, "强制刷新时清理本地工程信息缓存失败")
        End Try
        Select Case WhichPage
            Case CompType.Mod
                If FrmVersionMod IsNot Nothing Then FrmVersionMod.ReloadCompFileList(True) '无需 Else，还没加载刷个鬼的新
                FrmVersionLeft.ItemMod.Checked = True
            Case CompType.ResourcePack
                If FrmVersionResourcePack IsNot Nothing Then FrmVersionResourcePack.ReloadCompFileList(True)
                FrmVersionLeft.ItemResourcePack.Checked = True
            Case CompType.Shader
                If FrmVersionShader IsNot Nothing Then FrmVersionShader.ReloadCompFileList(True)
                FrmVersionLeft.ItemShader.Checked = True
            Case CompType.Schematic
                If FrmVersionSchematic IsNot Nothing Then FrmVersionSchematic.ReloadCompFileList(True)
                FrmVersionLeft.ItemSchematic.Checked = True
        End Select
        Hint("正在刷新……", Log:=False)
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, CompResourceListLoader, AddressOf LoadUIFromLoaderOutput, Function() CurrentCompType, AutoRun:=False)
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If CompResourceListLoader.State = LoadState.Failed Then
            LoaderRun(LoaderFolderRunType.ForceRun)
        End If
    End Sub
    Public Function LoaderRun(Type As LoaderFolderRunType) As Boolean
        Dim LoadPath As String
        If String.IsNullOrEmpty(CurrentFolderPath) Then
            '加载根目录
            LoadPath = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
        Else
            '加载当前文件夹
            LoadPath = CurrentFolderPath
        End If
        Return LoaderFolderRun(CompResourceListLoader, LoadPath, Type, LoaderInput:=GetRequireLoaderData())
    End Function

#End Region

#Region "文件夹导航"

    ''' <summary>
    ''' 当前显示的文件夹路径。空字符串表示根目录。
    ''' </summary>
    Public Property CurrentFolderPath As String = ""
    
    ''' <summary>
    ''' 进入指定的文件夹。
    ''' </summary>
    Private Sub EnterFolder(folderPath As String)
        Try
            If String.IsNullOrEmpty(folderPath) OrElse Not Directory.Exists(folderPath) Then
                Hint("文件夹不存在或已被删除", HintType.Critical)
                Return
            End If
            
            CurrentFolderPath = folderPath
            Log($"[原理图] 进入文件夹：{folderPath}")
            
            LoaderFolderRun(CompResourceListLoader, folderPath, LoaderFolderRunType.ForceRun, LoaderInput:=GetRequireLoaderData())
        Catch ex As Exception
            Log(ex, $"进入文件夹失败", LogLevel.Msgbox)
        End Try
    End Sub
    
    ''' <summary>
    ''' 检查文件夹是否为空，如果为空则提示用户，否则进入文件夹。
    ''' </summary>
    Private Sub EnterFolderWithCheck(folderPath As String)
        Try
            If String.IsNullOrEmpty(folderPath) OrElse Not Directory.Exists(folderPath) Then
                Hint("文件夹不存在或已被删除", HintType.Critical)
                Return
            End If
            
            '检查文件夹是否为空
            Dim hasFiles As Boolean = False
            Select Case CurrentCompType
                Case CompType.Schematic
                    hasFiles = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.AllDirectories).Any(Function(f) LocalCompFile.IsCompFile(f.FullName, CompType.Schematic))
                Case CompType.Mod
                    hasFiles = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(Function(f) LocalCompFile.IsCompFile(f.FullName, CompType.Mod))
                Case CompType.ResourcePack
                    hasFiles = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(Function(f) LocalCompFile.IsCompFile(f.FullName, CompType.ResourcePack))
                Case CompType.Shader
                    hasFiles = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(Function(f) LocalCompFile.IsCompFile(f.FullName, CompType.Shader))
                Case Else
                    hasFiles = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any()
            End Select
            
            If Not hasFiles Then
                Hint("该文件夹内没有文件。")
                Return
            End If
            
            '文件夹不为空，进入文件夹
            EnterFolder(folderPath)
        Catch ex As Exception
            Log(ex, $"检查文件夹失败", LogLevel.Msgbox)
        End Try
    End Sub
    
    ''' <summary>
    ''' 返回上级文件夹。
    ''' </summary>
    Private Sub GoBackToParentFolder()
        If String.IsNullOrEmpty(CurrentFolderPath) Then Return
        
        Try
            '获取根路径
            Dim rootPath = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
            rootPath = System.IO.Path.GetFullPath(rootPath.TrimEnd("\"))
            
            '获取父级路径
            Dim parentPath = Directory.GetParent(CurrentFolderPath)?.FullName
            
            '如果父级路径就是根路径或者父级路径不在根路径范围内，则返回根目录
            If parentPath Is Nothing OrElse parentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase) OrElse Not parentPath.StartsWith(rootPath & "\", StringComparison.OrdinalIgnoreCase) Then
                CurrentFolderPath = ""
            Else
                CurrentFolderPath = parentPath
            End If
        Catch ex As Exception
            Log(ex, $"路径处理失败")
            '发生错误时直接返回根目录
            CurrentFolderPath = ""
        End Try
        
        Log($"[原理图] 返回上级文件夹：{If(String.IsNullOrEmpty(CurrentFolderPath), "根目录", CurrentFolderPath)}")
        
        '重新加载当前文件夹的内容
        Dim LoadPath As String
        If String.IsNullOrEmpty(CurrentFolderPath) Then
            '返回到根目录
            LoadPath = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
        Else
            '加载当前文件夹
            LoadPath = CurrentFolderPath
        End If
        
        '强制刷新UI状态
        RunInUi(Sub()
                   '确保按钮状态正确
                   BtnManageBack.Visibility = If(Not String.IsNullOrEmpty(CurrentFolderPath), Visibility.Visible, Visibility.Collapsed)
               End Sub)
        
        '延迟一帧后再加载，确保UI状态已更新
        RunInUi(Sub()
                   LoaderFolderRun(CompResourceListLoader, LoadPath, LoaderFolderRunType.ForceRun, LoaderInput:=GetRequireLoaderData())
               End Sub, True)
    End Sub

#End Region

#Region "UI 化"

    ''' <summary>
    ''' 已加载的 Mod UI 缓存，不确保按显示顺序排列。Key 为 Mod 的 RawFileName。
    ''' </summary>
    Public ModItems As New Dictionary(Of String, MyLocalCompItem)
    ''' <summary>
    ''' 将加载器结果的 Mod 列表加载为 UI。
    ''' </summary>
    Private Sub LoadUIFromLoaderOutput()
        Try
            '判断应该显示哪一个页面
            If CompResourceListLoader.Output.Any() Then
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
                PanSchematicEmpty.Visibility = Visibility.Collapsed
            Else
                '检查是否为投影文件类型且schematics文件夹不存在
                If CurrentCompType = CompType.Schematic Then
                    Dim schematicsPath As String = PageVersionLeft.Version.PathIndie & "schematics\"
                    If Not Directory.Exists(schematicsPath) Then
                        PanSchematicEmpty.Visibility = Visibility.Visible
                        PanEmpty.Visibility = Visibility.Collapsed
                        PanBack.Visibility = Visibility.Collapsed
                        Return
                    End If
                End If
                
                '根据组件类型设置PanEmpty的文本内容
                If CurrentCompType = CompType.Schematic Then
                    TxtEmptyTitle.Text = "尚未安装资源"
                    TxtEmptyDescription.Text = "你可以从已经下载好的文件安装资源。" & vbCrLf &  "如果你已经安装了资源，可能是版本隔离设置有误，请在设置中调整版本隔离选项。"
                Else
                    TxtEmptyTitle.Text = "尚未安装资源"
                    TxtEmptyDescription.Text = "你可以下载新的资源，也可以从已经下载好的文件安装资源。" & vbCrLf & "如果你已经安装了资源，可能是版本隔离设置有误，请在设置中调整版本隔离选项。"
                End If
                
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                PanSchematicEmpty.Visibility = Visibility.Collapsed
                Return
            End If
            '修改缓存
            ModItems.Clear()
            For Each ModEntity As LocalCompFile In CompResourceListLoader.Output
                ModItems(ModEntity.RawFileName) = BuildLocalCompItem(ModEntity)
            Next
            '显示结果
            RunInUi(Sub()
                        Filter = FilterType.All
                        SearchBox.Text = "" '这会触发结果刷新，所以需要在 ModItems 更新之后，详见 #3124 的视频
                        RefreshUI()
                        SetSortMethod(SortMethod.CompName)
                    End Sub)
        Catch ex As Exception
            Log(ex, $"加载 {CurrentCompType} 列表 UI 失败", LogLevel.Feedback)
        End Try
    End Sub
    Private Function BuildLocalCompItem(Entry As LocalCompFile) As MyLocalCompItem
        Try
            AniControlEnabled += 1
            Dim NewItem As New MyLocalCompItem With {.SnapsToDevicePixels = True, .Entry = Entry,
                .ButtonHandler = AddressOf BuildLocalCompItemBtnHandler, .Checked = SelectedMods.Contains(Entry.RawFileName)}
            NewItem.CurrentSwipe = CurrentSwipSelect
            AddHandler Entry.OnCompUpdate, AddressOf NewItem.Refresh
            'AddHandler Entry.OnCompUpdate, Sub() RunInUi(Sub() DoSort())
            NewItem.Refresh()
            AniControlEnabled -= 1
            Return NewItem
        Catch ex As Exception
            AniControlEnabled -= 1
            Log(ex, $"创建UI项失败：{Entry.RawFileName}", LogLevel.Debug)
            Throw
        End Try
    End Function
    Private Sub BuildLocalCompItemBtnHandler(sender As MyLocalCompItem, e As EventArgs)
        '点击事件
        AddHandler sender.Changed, AddressOf CheckChanged
        If sender.Entry.IsFolder Then
            '文件夹项的点击事件：进入文件夹
            AddHandler sender.Click, Sub(ss As MyLocalCompItem, ee As EventArgs) EnterFolderWithCheck(ss.Entry.ActualPath)
        Else
            '文件项的点击事件：切换选中状态
            AddHandler sender.Click, Sub(ss As MyLocalCompItem, ee As EventArgs) ss.Checked = Not ss.Checked
        End If
        '图标按钮
        Dim BtnOpen As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonOpen, .Tag = sender}
        BtnOpen.ToolTip = "打开文件位置"
        ToolTipService.SetPlacement(BtnOpen, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnOpen, 30)
        ToolTipService.SetHorizontalOffset(BtnOpen, 2)
        AddHandler BtnOpen.Click, AddressOf Open_Click
        Dim BtnCont As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonInfo, .Tag = sender}
        BtnCont.ToolTip = "详情"
        ToolTipService.SetPlacement(BtnCont, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnCont, 30)
        ToolTipService.SetHorizontalOffset(BtnCont, 2)
        AddHandler BtnCont.Click, AddressOf Info_Click
        AddHandler sender.MouseRightButtonUp, AddressOf Info_Click
        Dim BtnDelete As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonDelete, .Tag = sender}
        BtnDelete.ToolTip = "删除"
        ToolTipService.SetPlacement(BtnDelete, Primitives.PlacementMode.Center)
        ToolTipService.SetVerticalOffset(BtnDelete, 30)
        ToolTipService.SetHorizontalOffset(BtnDelete, 2)
        AddHandler BtnDelete.Click, AddressOf Delete_Click
        If CurrentCompType <> CompType.Mod OrElse sender.Entry.State = LocalCompFile.LocalFileStatus.Unavailable Then
            sender.Buttons = {BtnCont, BtnOpen, BtnDelete}
        Else
            Dim BtnED As New MyIconButton With {.LogoScale = 1, .Logo = If(sender.Entry.State = LocalCompFile.LocalFileStatus.Fine, Logo.IconButtonStop, Logo.IconButtonCheck), .Tag = sender}
            BtnED.ToolTip = If(sender.Entry.State = LocalCompFile.LocalFileStatus.Fine, "禁用", "启用")
            ToolTipService.SetPlacement(BtnED, Primitives.PlacementMode.Center)
            ToolTipService.SetVerticalOffset(BtnED, 30)
            ToolTipService.SetHorizontalOffset(BtnED, 2)
            AddHandler BtnED.Click, AddressOf ED_Click
            sender.Buttons = {BtnCont, BtnOpen, BtnED, BtnDelete}
        End If
    End Sub

    ''' <summary>
    ''' 刷新整个 UI。
    ''' </summary>
    Public Sub RefreshUI()
        If PanList Is Nothing Then Return
        Dim ShowingMods = If(IsSearching, SearchResult, If(CompResourceListLoader.Output, New List(Of LocalCompFile))).Where(Function(m) CanPassFilter(m)).ToList
        '重新列出列表
        AniControlEnabled += 1
        If ShowingMods.Any() Then
            PanList.Visibility = Visibility.Visible
            PanList.Children.Clear()
            For Each TargetMod In ShowingMods
                If Not ModItems.ContainsKey(TargetMod.RawFileName) Then Continue For
                Dim Item As MyLocalCompItem = ModItems(TargetMod.RawFileName)
                MinecraftFormatter.SetColorfulTextLab(Item.LabTitle.Text, Item.LabTitle)
                MinecraftFormatter.SetColorfulTextLab(Item.LabInfo.Text, Item.LabInfo)
                Item.Checked = SelectedMods.Contains(TargetMod.RawFileName) '更新选中状态
                PanList.Children.Add(Item)
            Next
        Else
            PanList.Visibility = Visibility.Collapsed
        End If
        AniControlEnabled -= 1
        SelectedMods = SelectedMods.Where(Function(m) ShowingMods.Any(Function(s) s.RawFileName = m)).ToList '取消选中已经不显示的 Mod
        RefreshBars()
    End Sub

    ''' <summary>
    ''' 刷新顶栏和底栏显示。
    ''' </summary>
    Public Sub RefreshBars()
        '-----------------
        ' 顶部栏
        '-----------------

        '计数
        Dim AnyCount As Integer = 0
        Dim EnabledCount As Integer = 0
        Dim DisabledCount As Integer = 0
        Dim UpdateCount As Integer = 0
        Dim UnavalialeCount As Integer = 0
        Dim ItemSource = If(IsSearching, SearchResult, If(CompResourceListLoader.Output, New List(Of LocalCompFile)))
        For Each ModItem In ItemSource
            AnyCount += 1
            If ModItem.CanUpdate Then UpdateCount += 1
            If ModItem.State.Equals(LocalCompFile.LocalFileStatus.Fine) Then EnabledCount += 1
            If ModItem.State.Equals(LocalCompFile.LocalFileStatus.Disabled) Then DisabledCount += 1
            If ModItem.State.Equals(LocalCompFile.LocalFileStatus.Unavailable) Then UnavalialeCount += 1
        Next
        '显示
        BtnFilterAll.Text = If(IsSearching, "搜索结果", "全部") & $" ({AnyCount})"
        BtnFilterCanUpdate.Text = $"可更新 ({UpdateCount})"
        BtnFilterCanUpdate.Visibility = If(Filter = FilterType.CanUpdate OrElse UpdateCount > 0, Visibility.Visible, Visibility.Collapsed)
        BtnFilterEnabled.Text = $"启用 ({EnabledCount})"
        BtnFilterEnabled.Visibility = If(Filter = FilterType.Enabled OrElse (EnabledCount > 0 AndAlso EnabledCount < AnyCount), Visibility.Visible, Visibility.Collapsed)
        BtnFilterDisabled.Text = $"禁用 ({DisabledCount})"
        BtnFilterDisabled.Visibility = If(Filter = FilterType.Disabled OrElse DisabledCount > 0, Visibility.Visible, Visibility.Collapsed)
        BtnFilterError.Text = $"错误 ({UnavalialeCount})"
        BtnFilterError.Visibility = If(Filter = FilterType.Unavailable OrElse UnavalialeCount > 0, Visibility.Visible, Visibility.Collapsed)
        '查找重复项目
        Dim DuplicateItems = ItemSource.GroupBy(Function(m)
                                                    If m.Comp Is Nothing Then
                                                        Return ":Nothing:"
                                                    Else
                                                        Return m.Comp.Id
                                                    End If
                                                End Function).Where(Function(g) g.Count > 1 AndAlso g.First.Comp IsNot Nothing).SelectMany(Function(g) g).ToList()
        BtnFilterDuplicate.Text = $"重复 ({DuplicateItems.Count})"
        BtnFilterDuplicate.Visibility = If(Filter = FilterType.Duplicate OrElse DuplicateItems.Any, Visibility.Visible, Visibility.Collapsed)
        
        '返回按钮显示控制（在子文件夹中时显示）
        If Not String.IsNullOrEmpty(CurrentFolderPath) Then
            BtnManageBack.Visibility = Visibility.Visible
        Else
            BtnManageBack.Visibility = Visibility.Collapsed
        End If

        '-----------------
        ' 底部栏
        '-----------------

        '计数
        Dim NewCount As Integer = SelectedMods.Count
        Dim Selected = NewCount > 0
        If Selected Then LabSelect.Text = $"已选择 {NewCount} 个文件" '取消所有选择时不更新数字
        '按钮可用性
        If Selected Then
            Dim HasUpdate As Boolean = False
            Dim HasEnabled As Boolean = False
            Dim HasDisabled As Boolean = False
            For Each ModEntity In CompResourceListLoader.Output
                If SelectedMods.Contains(ModEntity.RawFileName) Then
                    If ModEntity.CanUpdate Then HasUpdate = True
                    If ModEntity.State = LocalCompFile.LocalFileStatus.Fine Then
                        HasEnabled = True
                    ElseIf ModEntity.State = LocalCompFile.LocalFileStatus.Disabled Then
                        HasDisabled = True
                    End If
                End If
            Next
            BtnSelectDisable.IsEnabled = HasEnabled
            BtnSelectEnable.IsEnabled = HasDisabled
            BtnSelectUpdate.IsEnabled = HasUpdate
        End If
        '更新显示状态
        If AniControlEnabled = 0 Then
            PanListBack.Margin = New Thickness(0, 0, 0, If(Selected, 95, 15))
            If Selected Then
                '仅在数量增加时播放出现/跳跃动画
                If BottomBarShownCount >= NewCount Then
                    BottomBarShownCount = NewCount
                    Return
                Else
                    BottomBarShownCount = NewCount
                End If
                '出现/跳跃动画
                CardSelect.Visibility = Visibility.Visible
                AniStart({
                    AaOpacity(CardSelect, 1 - CardSelect.Opacity, 60),
                    AaTranslateY(CardSelect, -27 - TransSelect.Y, 120, Ease:=New AniEaseOutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, 3, 150, 120, Ease:=New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, -1, 90, 270, Ease:=New AniEaseInoutFluent(AniEasePower.Weak))
                }, "Mod Sidebar")
            Else
                '不重复播放隐藏动画
                If BottomBarShownCount = 0 Then Return
                BottomBarShownCount = 0
                '隐藏动画
                AniStart({
                    AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                    AaTranslateY(CardSelect, -10 - TransSelect.Y, 90, Ease:=New AniEaseInFluent(AniEasePower.Weak)),
                    AaCode(Sub() CardSelect.Visibility = Visibility.Collapsed, After:=True)
                }, "Mod Sidebar")
            End If
        Else
            AniStop("Mod Sidebar")
            BottomBarShownCount = NewCount
            If Selected Then
                CardSelect.Visibility = Visibility.Visible
                CardSelect.Opacity = 1
                TransSelect.Y = -25
            Else
                CardSelect.Visibility = Visibility.Collapsed
                CardSelect.Opacity = 0
                TransSelect.Y = -10
            End If
        End If
    End Sub
    Private BottomBarShownCount As Integer = 0

#End Region

#Region "管理"

    ''' <summary>
    ''' 打开 Mods 文件夹。
    ''' </summary>
    Private Sub BtnManageBack_Click(sender As Object, e As EventArgs) Handles BtnManageBack.Click
        GoBackToParentFolder()
    End Sub

    Private Sub BtnManageOpen_Click(sender As Object, e As EventArgs) Handles BtnManageOpen.Click, BtnHintOpen.Click
        Try
            Dim CompFilePath = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
            Directory.CreateDirectory(CompFilePath)
            OpenExplorer(CompFilePath)
        Catch ex As Exception
            Log(ex, "打开 Mods 文件夹失败", LogLevel.Msgbox)
        End Try
    End Sub

    ''' <summary>
    ''' 全选。
    ''' </summary>
    Private Sub BtnManageSelectAll_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageSelectAll.Click
        ChangeAllSelected(SelectedMods.Count < PanList.Children.Count)
    End Sub

    ''' <summary>
    ''' 安装 Mod。
    ''' </summary>
    Private Sub BtnManageInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageInstall.Click, BtnHintInstall.Click
        Dim FileList As String() = Nothing
        Select Case CurrentCompType
            Case CompType.Mod : FileList = SelectFiles("Mod 文件(*.jar;*.litemod;*.disabled;*.old)|*.jar;*.litemod;*.disabled;*.old", "选择要安装的 Mod")
            Case CompType.ResourcePack : FileList = SelectFiles("资源包文件(*.zip)|*.zip", "选择要安装的资源包")
            Case CompType.Shader : FileList = SelectFiles("光影包文件(*.zip)|*.zip", "选择要安装的光影包")
            Case CompType.Schematic : FileList = SelectFiles("投影原理图文件(*.litematic;*.nbt;*.schematic)|*.litematic;*.nbt;*.schematic", "选择要安装的投影原理图")
        End Select
        If FileList Is Nothing OrElse Not FileList.Any Then Exit Sub
        InstallCompFiles(FileList, CurrentCompType)
    End Sub
    ''' <summary>
    ''' 尝试安装 Mod。
    ''' 返回输入的文件是否为一个 Mod 文件，仅用于判断拖拽行为。
    ''' </summary>
    Public Shared Function InstallMods(FilePathList As IEnumerable(Of String)) As Boolean
        Dim Extension As String = FilePathList.First.AfterLast(".").ToLower
        '检查文件扩展名
        If Not {"jar", "litemod", "disabled", "old"}.Any(Function(t) t = Extension) Then Return False
        Log("[System] 文件为 jar/litemod 格式，尝试作为 Mod 安装")
        '检查回收站：回收站中的文件有错误的文件名
        If FilePathList.First.Contains(":\$RECYCLE.BIN\") Then
            Hint("请先将文件从回收站还原，再尝试安装！", HintType.Critical)
            Return True
        End If
        '获取并检查目标版本
        Dim TargetVersion As McVersion = McVersionCurrent
        Dim ModFolder = TargetVersion.PathIndie & If(TargetVersion.Version.HasLabyMod, "labymod-neo\fabric\" & TargetVersion.Version.McName & "\", "") & "mods\"
        If FrmMain.PageCurrent = FormMain.PageType.VersionSetup Then TargetVersion = PageVersionLeft.Version
        If FrmMain.PageCurrent = FormMain.PageType.VersionSelect OrElse TargetVersion Is Nothing OrElse Not TargetVersion.Modable Then
            '正在选择版本，或当前版本不能安装 Mod
            Hint("若要安装 Mod，请先选择一个可以安装 Mod 的版本！")
        ElseIf Not (FrmMain.PageCurrent = FormMain.PageType.VersionSetup AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.VersionMod) Then
            '未处于 Mod 管理页面
            If MyMsgBox($"是否要将这{If(FilePathList.Count = 1, "个", "些")}文件作为 Mod 安装到 {TargetVersion.Name}？", "Mod 安装确认", "确定", "取消") = 1 Then GoTo Install
        Else
            '处于 Mod 管理页面
Install:
            Try
                For Each ModFile In FilePathList
                    Dim NewFileName = GetFileNameFromPath(ModFile).Replace(".disabled", "").Replace(".old", "")
                    If Not NewFileName.Contains(".") Then NewFileName += ".jar" '#4227
                    CopyFile(ModFile, ModFolder & NewFileName)
                Next
                If FilePathList.Count = 1 Then
                    Hint($"已安装 {GetFileNameFromPath(FilePathList.First).Replace(".disabled", "").Replace(".old", "")}！", HintType.Finish)
                Else
                    Hint($"已安装 {FilePathList.Count} 个 Mod！", HintType.Finish)
                End If
                '刷新列表
                If FrmMain.PageCurrent = FormMain.PageType.VersionSetup AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.VersionMod Then
                    LoaderFolderRun(CompResourceListLoader, ModFolder, LoaderFolderRunType.ForceRun, LoaderInput:=FrmVersionMod?.GetRequireLoaderData())
                End If
            Catch ex As Exception
                Log(ex, "复制 Mod 文件失败", LogLevel.Msgbox)
            End Try
        End If
        Return True
    End Function

    ''' <summary>
    ''' 安装组件文件（Mod、资源包、光影包、投影文件等）。
    ''' </summary>
    Public Shared Sub InstallCompFiles(FilePathList As IEnumerable(Of String), CompType As CompType)
        If Not FilePathList.Any Then Exit Sub
        
        Dim Extension As String = FilePathList.First.AfterLast(".").ToLower
        Dim ValidExtensions As String() = Nothing
        Dim CompTypeName As String = ""
        Dim CompFolder As String = ""
        
        '检查回收站：回收站中的文件有错误的文件名
        If FilePathList.First.Contains(":\$RECYCLE.BIN\") Then
            Hint("请先将文件从回收站还原，再尝试安装！", HintType.Critical)
            Exit Sub
        End If
        
        '获取并检查目标版本
        Dim TargetVersion As McVersion = McVersionCurrent
        If FrmMain.PageCurrent = FormMain.PageType.VersionSetup Then TargetVersion = PageVersionLeft.Version
        
        '根据组件类型设置相关参数
        Select Case CompType
            Case CompType.Mod
                ValidExtensions = {"jar", "litemod", "disabled", "old"}
                CompTypeName = "Mod"
                CompFolder = TargetVersion.PathIndie & If(TargetVersion.Version.HasLabyMod, "labymod-neo\fabric\" & TargetVersion.Version.McName & "\", "") & "mods\"
            Case CompType.ResourcePack
                ValidExtensions = {"zip"}
                CompTypeName = "资源包"
                CompFolder = TargetVersion.PathIndie & "resourcepacks\"
            Case CompType.Shader
                ValidExtensions = {"zip"}
                CompTypeName = "光影包"
                CompFolder = TargetVersion.PathIndie & "shaderpacks\"
            Case CompType.Schematic
                ValidExtensions = {"litematic", "nbt", "schematic"}
                CompTypeName = "投影原理图"
                CompFolder = TargetVersion.PathIndie & "schematics\"
        End Select
        
        '检查文件扩展名
        If Not ValidExtensions.Contains(Extension) Then
            Hint($"不支持的文件格式：{Extension}，{CompTypeName}支持的格式：{String.Join(", ", ValidExtensions)}", HintType.Critical)
            Exit Sub
        End If
        
        Log($"[System] 文件为 {Extension} 格式，尝试作为{CompTypeName}安装")
        
        '检查版本兼容性
        If CompType = CompType.Mod AndAlso (FrmMain.PageCurrent = FormMain.PageType.VersionSelect OrElse TargetVersion Is Nothing OrElse Not TargetVersion.Modable) Then
            Hint("若要安装 Mod，请先选择一个可以安装 Mod 的版本！")
            Exit Sub
        End If
        
        '确认安装
        Dim CurrentPage As FormMain.PageSubType = FormMain.PageSubType.VersionMod
        Select Case CompType
            Case CompType.Mod : CurrentPage = FormMain.PageSubType.VersionMod
            Case CompType.ResourcePack : CurrentPage = FormMain.PageSubType.VersionResourcePack
            Case CompType.Shader : CurrentPage = FormMain.PageSubType.VersionShader
            Case CompType.Schematic : CurrentPage = FormMain.PageSubType.VersionSchematic
        End Select
        
        If Not (FrmMain.PageCurrent = FormMain.PageType.VersionSetup AndAlso FrmMain.PageCurrentSub = CurrentPage) Then
            If MyMsgBox($"是否要将这{If(FilePathList.Count = 1, "个", "些")}文件作为{CompTypeName}安装到 {TargetVersion.Name}？", $"{CompTypeName}安装确认", "确定", "取消") <> 1 Then Exit Sub
        End If
        
        '执行安装
        Try
            Directory.CreateDirectory(CompFolder)
            For Each FilePath In FilePathList
                Dim NewFileName = GetFileNameFromPath(FilePath)
                If CompType = CompType.Mod Then
                    NewFileName = NewFileName.Replace(".disabled", "").Replace(".old", "")
                    If Not NewFileName.Contains(".") Then NewFileName += ".jar"
                End If
                
                Dim DestFile = CompFolder & NewFileName
                If File.Exists(DestFile) Then
                    If MyMsgBox($"已存在同名文件：{NewFileName}，是否要覆盖？", "文件覆盖确认", "覆盖", "取消") <> 1 Then Continue For
                End If
                
                CopyFile(FilePath, DestFile)
            Next
            
            If FilePathList.Count = 1 Then
                Hint($"已安装 {GetFileNameFromPath(FilePathList.First)}！", HintType.Finish)
            Else
                Hint($"已安装 {FilePathList.Count} 个{CompTypeName}！", HintType.Finish)
            End If
            
            '刷新列表
            If FrmMain.PageCurrent = FormMain.PageType.VersionSetup AndAlso FrmMain.PageCurrentSub = CurrentPage Then
                Select Case CompType
                    Case CompType.Mod
                        If FrmVersionMod IsNot Nothing Then
                            LoaderFolderRun(CompResourceListLoader, CompFolder, LoaderFolderRunType.ForceRun, LoaderInput:=FrmVersionMod?.GetRequireLoaderData())
                        End If
                    Case CompType.ResourcePack, CompType.Shader, CompType.Schematic
                        Dim CurrentForm = GetCurrentCompResourceForm()
                        If CurrentForm IsNot Nothing Then
                            RunInUi(Sub() CurrentForm.ReloadCompFileList(True))
                        End If
                End Select
            End If
            
        Catch ex As Exception
            Log(ex, $"复制{CompTypeName}文件失败", LogLevel.Msgbox)
        End Try
    End Sub
    
    ''' <summary>
    ''' 获取当前的组件资源管理窗体。
    ''' </summary>
    Private Shared Function GetCurrentCompResourceForm() As PageVersionCompResource
        Select Case FrmMain.PageCurrentSub
            Case FormMain.PageSubType.VersionMod : Return FrmVersionMod
            Case FormMain.PageSubType.VersionResourcePack : Return FrmVersionResourcePack
            Case FormMain.PageSubType.VersionShader : Return FrmVersionShader
            Case FormMain.PageSubType.VersionSchematic : Return FrmVersionSchematic
            Case Else : Return Nothing
        End Select
    End Function

    Private Sub BtnManageInfoExport_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageInfoExport.Click
        Dim Choice = MyMsgBox("TXT 格式：仅导出当前的资源文件名称信息，通常足够他人获取已安装的资源信息" & vbCrLf &
                              "CSV 格式：导出详细的资源信息，包括其文件名，工程的 ID，文件内版本信息等详细信息",
                                Title:="选择导出模式",
                                Button1:="TXT 格式",
                                Button2:="CSV 格式",
                                Button3:="取消")
        Dim ExportText = Sub(Content As String, FileName As String)
                             Try
                                 Dim savePath = SelectSaveFile("选择保存位置", FileName, "文本文件(*.txt)|*.txt|CSV 文件(*.csv)|*.csv")
                                 If String.IsNullOrWhiteSpace(savePath) Then Exit Sub
                                 File.WriteAllText(savePath, Content, Encoding.UTF8)
                                 OpenExplorer(savePath)
                             Catch ex As Exception
                                 Log(ex, "导出资源信息失败", LogLevel.Msgbox)
                             End Try
                         End Sub
        Select Case Choice
            Case 1 'TXT
                Dim ExportContent As New List(Of String)
                For Each ModEntity In CompResourceListLoader.Output
                    ExportContent.Add(ModEntity.FileName)
                Next
                ExportText(Join(ExportContent, vbCrLf), PageVersionLeft.Version.Name & "已安装的资源信息.txt")

            Case 2 'CSV
                Dim ExportContent As New List(Of String)
                ExportContent.Add("文件名,资源名称,资源版本,此版本更新时间,Mod ID,对应平台工程 ID,文件大小（字节）,文件路径")
                For Each ModEntity In CompResourceListLoader.Output
                    ExportContent.Add($"{ModEntity.FileName},{ModEntity.Comp?.TranslatedName},{ModEntity.Version},{ModEntity.CompFile?.ReleaseDate},{ModEntity.ModId},{ModEntity.Comp?.Id},{New FileInfo(ModEntity.Path).Length},{ModEntity.Path}")
                Next
                ExportText(Join(ExportContent, vbCrLf), PageVersionLeft.Version.Name & "已安装的资源信息.csv")

        End Select
    End Sub

    ''' <summary>
    ''' 下载 Mod。
    ''' </summary>
    Private Sub BtnManageDownload_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageDownload.Click, BtnHintDownload.Click
        PageComp.TargetVersion = PageVersionLeft.Version '将当前版本设置为筛选器
        Select Case CurrentCompType
            Case CompType.Mod : FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod)
            Case CompType.ResourcePack : FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadResourcePack)
            Case CompType.Shader : FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadShader)
        End Select
    End Sub

    ''' <summary>
    ''' 下载投影Mod按钮点击事件。
    ''' </summary>
    Private Sub BtnSchematicDownloadMod_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSchematicDownloadMod.Click
        PageComp.TargetVersion = PageVersionLeft.Version '将当前版本设置为筛选器
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod)
    End Sub

    ''' <summary>
    ''' 版本选择按钮点击事件。
    ''' </summary>
    Private Sub BtnSchematicVersionSelect_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSchematicVersionSelect.Click
        FrmMain.PageChange(FormMain.PageType.VersionSelect)
    End Sub

#End Region

#Region "选择"

    ''' <summary>
    ''' 选择的 Mod 的路径（不含 .disabled 和 .old）。
    ''' </summary>
    Public SelectedMods As New List(Of String)

    '单项切换选择状态
    Public Sub CheckChanged(sender As MyLocalCompItem, e As RouteEventArgs)
        If AniControlEnabled <> 0 Then Return
        '更新选择了的内容
        Dim SelectedKey As String = sender.Entry.RawFileName
        If sender.Checked Then
            If Not SelectedMods.Contains(SelectedKey) Then SelectedMods.Add(SelectedKey)
        Else
            SelectedMods.Remove(SelectedKey)
        End If
        RefreshBars()
    End Sub

    '切换所有项的选择状态
    Private Sub ChangeAllSelected(Value As Boolean)
        AniControlEnabled += 1
        SelectedMods.Clear()
        For Each Item As MyLocalCompItem In ModItems.Values
            '#4992，Mod 从过滤器看可能不应在列表中，但因为刚切换状态所以依然保留在列表中，所以应该从列表 UI 判断，而非从过滤器判断
            Dim ShouldSelected As Boolean = Value AndAlso PanList.Children.Contains(Item)
            Item.Checked = ShouldSelected
            If ShouldSelected Then SelectedMods.Add(Item.Entry.RawFileName)
        Next
        AniControlEnabled -= 1
        RefreshBars()
    End Sub
    Private Sub UnselectedAllWithAnimation() Handles Load.StateChanged, Me.PageExit
        Dim CacheAniControlEnabled = AniControlEnabled
        AniControlEnabled = 0
        ChangeAllSelected(False)
        AniControlEnabled += CacheAniControlEnabled
    End Sub
    Private Sub FrmMain_KeyDown(sender As Object, e As KeyEventArgs) '若监听自己的事件则在进入页面后需点击右侧控件才可监听到 (#4311)
        If FrmMain.PageRight IsNot Me Then Return
        If My.Computer.Keyboard.CtrlKeyDown AndAlso e.Key = Key.A Then ChangeAllSelected(True)
    End Sub

#End Region

#Region "筛选"

    Private _Filter As FilterType = FilterType.All
    Public Property Filter As FilterType
        Get
            Return _Filter
        End Get
        Set(value As FilterType)
            If _Filter = value Then Return
            _Filter = value
            Select Case value
                Case FilterType.All
                    BtnFilterAll.Checked = True
                Case FilterType.Enabled
                    BtnFilterEnabled.Checked = True
                Case FilterType.Disabled
                    BtnFilterDisabled.Checked = True
                Case FilterType.CanUpdate
                    BtnFilterCanUpdate.Checked = True
                Case FilterType.Duplicate
                    BtnFilterDuplicate.Checked = True
                Case Else
                    BtnFilterError.Checked = True
            End Select
            RefreshUI()
        End Set
    End Property
    Public Enum FilterType As Integer
        All = 0
        Enabled = 1
        Disabled = 2
        CanUpdate = 3
        Unavailable = 4
        Duplicate = 5
    End Enum

    ''' <summary>
    ''' 检查该 Mod 项是否符合当前筛选的类别。
    ''' </summary>
    Private Function CanPassFilter(CheckingMod As LocalCompFile) As Boolean
        Select Case Filter
            Case FilterType.All
                Return True
            Case FilterType.Enabled
                Return CheckingMod.State = LocalCompFile.LocalFileStatus.Fine
            Case FilterType.Disabled
                Return CheckingMod.State = LocalCompFile.LocalFileStatus.Disabled
            Case FilterType.CanUpdate
                Return CheckingMod.CanUpdate
            Case FilterType.Unavailable
                Return CheckingMod.State = LocalCompFile.LocalFileStatus.Unavailable
            Case FilterType.Duplicate
                Dim ItemSource = If(IsSearching, SearchResult, If(CompResourceListLoader.Output, New List(Of LocalCompFile)))
                Return ItemSource IsNot Nothing AndAlso ItemSource.Where(Function(m) CheckingMod.Comp IsNot Nothing AndAlso m.Comp IsNot Nothing AndAlso CheckingMod.Comp.Id = m.Comp.Id).Count > 1
            Case Else
                Return False
        End Select
    End Function

    '点击筛选项触发的改变
    Private Sub ChangeFilter(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnFilterAll.Check, BtnFilterCanUpdate.Check, BtnFilterDisabled.Check, BtnFilterEnabled.Check, BtnFilterError.Check, BtnFilterDuplicate.Check
        Filter = sender.Tag
        RefreshUI()
        DoSort()
    End Sub

#End Region

#Region "排序"
    Private CurrentSortMethod As SortMethod = SortMethod.CompName

    Private Sub SetSortMethod(Target As SortMethod)
        CurrentSortMethod = Target
        BtnSort.Text = $"排序：{GetSortName(Target)}"
        RefreshUI()
        DoSort()
    End Sub

    Private Enum SortMethod
        FileName
        CompName
        TagNums
        CreateTime
        ModFileSize
    End Enum

    Private Function GetSortName(Method As SortMethod) As String
        Select Case Method
            Case SortMethod.FileName : Return "文件名"
            Case SortMethod.CompName : Return "资源名称"
            Case SortMethod.TagNums : Return "标签数量"
            Case SortMethod.CreateTime : Return "加入时间"
            Case SortMethod.ModFileSize : Return "文件大小"
            Case Else : Return "资源名称"
        End Select
        Return ""
    End Function

    Private Sub BtnSortClick(sender As Object, e As RouteEventArgs) Handles BtnSort.Click
        Dim Body As New ContextMenu
        For Each i As SortMethod In [Enum].GetValues(GetType(SortMethod))
            Dim Item As New MyMenuItem
            Item.Header = GetSortName(i)
            AddHandler Item.Click, Sub()
                                       SetSortMethod(i)
                                   End Sub
            Body.Items.Add(Item)
        Next
        Body.PlacementTarget = sender
        Body.Placement = Primitives.PlacementMode.Bottom
        Body.IsOpen = True
    End Sub

    Private ReadOnly SortLock As New Object
    Private Sub DoSort()
        SyncLock SortLock
            Try
                If PanList Is Nothing OrElse PanList.Children.Count < 2 Then Exit Sub

                ' 将子元素转换为可排序的列表
                Dim items = PanList.Children.OfType(Of MyLocalCompItem)().ToList()
                Dim Method = GetSortMethod(CurrentSortMethod)

                ' 分离有效和无效项（保持原始相对顺序）
                Dim invalid = items.Where(Function(i) i.Entry Is Nothing OrElse (CurrentSortMethod = SortMethod.TagNums AndAlso i.Entry.Comp Is Nothing)).ToList()
                Dim valid = items.Except(invalid).ToList()
                ' 仅对有效项进行排序
                valid.Sort(Function(x, y) Method(x.Entry, y.Entry))
                ' 合并保持无效项的原始顺序
                items = valid.Concat(invalid).ToList()

                ' 批量更新UI元素
                PanList.Children.Clear()
                items.ForEach(Sub(i) PanList.Children.Add(i))

            Catch ex As Exception
                Log(ex, "执行排序时出错", LogLevel.Hint)
            End Try
        End SyncLock
    End Sub

    Private Function GetSortMethod(Method As SortMethod) As Func(Of LocalCompFile, LocalCompFile, Integer)
        Select Case Method
            Case SortMethod.FileName
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹或都是文件，则按文件名排序
                           Return String.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase)
                       End Function
            Case SortMethod.CompName
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹或都是文件，则按资源名称排序
                           Return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                       End Function
            Case SortMethod.TagNums
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹，则按名称排序；如果都是文件，则按标签数量排序
                           If a.IsFolder AndAlso b.IsFolder Then
                               Return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                           Else
                               Return b.Comp.Tags.Count - a.Comp.Tags.Count
                           End If
                       End Function
            Case SortMethod.CreateTime
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹或都是文件，则按创建时间排序
                           Dim aDate = New FileInfo(If(a.IsFolder, a.ActualPath, a.Path)).CreationTime
                           Dim bDate = New FileInfo(If(b.IsFolder, b.ActualPath, b.Path)).CreationTime
                           Return If(aDate = bDate, 0, If(aDate > bDate, -1, 1))
                       End Function
            Case SortMethod.ModFileSize
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹，则按名称排序；如果都是文件，则按文件大小排序
                           If a.IsFolder AndAlso b.IsFolder Then
                               Return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                           Else
                               Dim aSize As Long = (New FileInfo(a.ActualPath)).Length
                               Dim bSize As Long = (New FileInfo(b.ActualPath)).Length
                               Return bSize.CompareTo(aSize)
                           End If
                       End Function
            Case Else
                Return Function(a As LocalCompFile, b As LocalCompFile) As Integer
                           ' 文件夹始终排在最前面
                           If a.IsFolder AndAlso Not b.IsFolder Then Return -1
                           If Not a.IsFolder AndAlso b.IsFolder Then Return 1
                           ' 如果都是文件夹或都是文件，则按名称排序
                           Return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                       End Function
        End Select
    End Function
#End Region

#Region "下边栏"

    '启用 / 禁用
    Private Sub BtnSelectED_Click(sender As MyIconTextButton, e As RouteEventArgs) Handles BtnSelectEnable.Click, BtnSelectDisable.Click
        EDMods(CompResourceListLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName)),
               Not sender.Equals(BtnSelectDisable))
        ChangeAllSelected(False)
    End Sub
    Private Sub EDMods(ModList As IEnumerable(Of LocalCompFile), IsEnable As Boolean)
        Dim IsSuccessful As Boolean = True
        For Each ModE In ModList.ToList
            Dim ModEntity = ModE '仅用于去除迭代变量无法修改的限制
            Dim NewPath As String = Nothing
            If ModEntity.State = LocalCompFile.LocalFileStatus.Fine AndAlso Not IsEnable Then
                '禁用
                NewPath = ModEntity.Path & If(File.Exists(ModEntity.Path & ".old"), ".old", ".disabled")
            ElseIf ModEntity.State = LocalCompFile.LocalFileStatus.Disabled AndAlso IsEnable Then
                '启用
                NewPath = ModEntity.RawPath
            Else
                Continue For
            End If
            '重命名
            Try
                If File.Exists(NewPath) Then
                    If File.Exists(ModEntity.Path) Then
                        '同时存在两个名称的 Mod
                        If GetFileMD5(ModEntity.Path) <> GetFileMD5(NewPath) Then
                            MyMsgBox($"目前同时存在启用和禁用的两个 Mod 文件：{vbCrLf} - {NewPath}{vbCrLf} - {ModEntity.Path}{vbCrLf}{vbCrLf}注意，这两个文件的内容并不相同。{vbCrLf}在手动删除或重命名其中一个文件后，才能继续操作。", "存在文件冲突")
                            Continue For
                        End If
                    Else
                        '已经重命名过了
                        Log("[Mod] Mod 的状态已被切换", LogLevel.Debug)
                        Continue For
                    End If
                End If
                File.Delete(NewPath)
                FileSystem.Rename(ModEntity.Path, NewPath)
            Catch ex As FileNotFoundException
                Log(ex, $"未找到需要重命名的 Mod（{If(ModEntity.Path, "null")}）", LogLevel.Feedback)
                ReloadCompFileList(True)
                Return
            Catch ex As Exception
                Log(ex, $"重命名 Mod 失败（{If(ModEntity.Path, "null")}）")
                IsSuccessful = False
            End Try
            '更改 Loader 中的列表
            Dim NewModEntity As New LocalCompFile(NewPath)
            NewModEntity.FromJson(ModEntity.ToJson)
            If CompResourceListLoader.Output.Contains(ModEntity) Then
                Dim IndexOfLoader As Integer = CompResourceListLoader.Output.IndexOf(ModEntity)
                CompResourceListLoader.Output.RemoveAt(IndexOfLoader)
                CompResourceListLoader.Output.Insert(IndexOfLoader, NewModEntity)
            End If
            If SearchResult IsNot Nothing AndAlso SearchResult.Contains(ModEntity) Then '#4862
                Dim IndexOfResult As Integer = SearchResult.IndexOf(ModEntity)
                SearchResult.Remove(ModEntity)
                SearchResult.Insert(IndexOfResult, NewModEntity)
            End If
            '更改 UI 中的列表
            Try
                Dim NewItem As MyLocalCompItem = BuildLocalCompItem(NewModEntity)
                ModItems(ModEntity.RawFileName) = NewItem
                Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalCompItem).FirstOrDefault(Function(i) i.Entry Is ModEntity))
                If IndexOfUi = -1 Then Continue For '因为未知原因 Mod 的状态已经切换完了
                PanList.Children.RemoveAt(IndexOfUi)
                PanList.Children.Insert(IndexOfUi, NewItem)
            Catch ex As Exception
                Log(ex, $"更新UI列表项失败：{ModEntity.RawFileName}", LogLevel.Hint)
                Continue For
            End Try
        Next
        If IsSuccessful Then
            RefreshBars()
        Else
            Hint("由于文件被占用，Mod 的状态切换失败，请尝试关闭正在运行的游戏后再试！", HintType.Critical)
            ReloadCompFileList(True)
        End If
        LoaderRun(LoaderFolderRunType.UpdateOnly)
    End Sub

    '更新
    Private Sub BtnSelectUpdate_Click() Handles BtnSelectUpdate.Click
        Dim UpdateList As List(Of LocalCompFile) = CompResourceListLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName) AndAlso m.CanUpdate).ToList()
        If Not UpdateList.Any() Then Return
        UpdateResource(UpdateList)
        ChangeAllSelected(False)
    End Sub
    ''' <summary>
    ''' 记录正在进行 Mod 更新的 mods 文件夹路径。
    ''' </summary>
    Public Shared UpdatingVersions As New List(Of String)
    Public Sub UpdateResource(ModList As IEnumerable(Of LocalCompFile))
        '更新前警告
        If CurrentCompType = CompType.Mod AndAlso ((Not Setup.Get("HintUpdateMod")) OrElse ModList.Count >= 15) Then
            If MyMsgBox($"新版本 Mod 可能不兼容旧存档或者其他 Mod，这可能导致游戏崩溃，甚至永久损坏存档！{vbCrLf}如果你在游玩整合包，请千万不要自行更新 Mod！{vbCrLf}{vbCrLf}在更新前，请先备份存档，并检查 Mod 的更新日志。{vbCrLf}如果更新后出现问题，你也可以在回收站找回更新前的 Mod。", "Mod 更新警告", "我已了解风险，继续更新", "取消", IsWarn:=True) = 1 Then
                Setup.Set("HintUpdateMod", True)
            Else
                Return
            End If
        End If
        Try
            '构造下载信息
            ModList = ModList.ToList() '防止刷新影响迭代器
            Dim FileList As New List(Of NetFile)
            Dim FileCopyList As New Dictionary(Of String, String)
            For Each Entry As LocalCompFile In ModList
                Dim File As CompFile = Entry.UpdateFile
                If Not File.Available Then Continue For
                '确认更新后的文件名
                Dim CurrentReplaceName = Entry.CompFile.FileName.Replace(".jar", "").Replace(".old", "").Replace(".disabled", "")
                Dim NewestReplaceName = Entry.UpdateFile.FileName.Replace(".jar", "").Replace(".old", "").Replace(".disabled", "")
                Dim CurrentSegs = CurrentReplaceName.Split("-"c).ToList()
                Dim NewestSegs = NewestReplaceName.Split("-"c).ToList()
                Dim Shortened As Boolean = False
                Do While True '移除前导相同部分（不能移除所有相同项，这会导致例如 1.2-forge-2 和 1.3-forge-3 中间的 forge 被去掉，导致尝试替换 1.2-2）
                    If Not CurrentSegs.Any() OrElse Not NewestSegs.Any() Then Exit Do
                    If CurrentSegs.First <> NewestSegs.First Then Exit Do
                    CurrentSegs.RemoveAt(0)
                    NewestSegs.RemoveAt(0)
                    Shortened = True
                Loop
                Do While True '移除后导相同部分
                    If Not CurrentSegs.Any() OrElse Not NewestSegs.Any() Then Exit Do
                    If CurrentSegs.Last <> NewestSegs.Last Then Exit Do
                    CurrentSegs.RemoveAt(CurrentSegs.Count - 1)
                    NewestSegs.RemoveAt(NewestSegs.Count - 1)
                    Shortened = True
                Loop
                If Shortened AndAlso CurrentSegs.Any() AndAlso NewestSegs.Any() Then
                    CurrentReplaceName = Join(CurrentSegs, "-")
                    NewestReplaceName = Join(NewestSegs, "-")
                End If
                '添加到下载列表
                Dim TempAddress As String = PathTemp & "DownloadedComp\" & Entry.FileName.Replace(CurrentReplaceName, NewestReplaceName)
                Dim RealAddress As String = GetPathFromFullPath(Entry.Path) & Entry.FileName.Replace(CurrentReplaceName, NewestReplaceName)
                FileList.Add(File.ToNetFile(TempAddress))
                FileCopyList(TempAddress) = RealAddress
            Next
            '构造加载器
            Dim InstallLoaders As New List(Of LoaderBase)
            Dim FinishedFileNames As New List(Of String)
            InstallLoaders.Add(New LoaderDownload("下载新版资源文件", FileList) With {.ProgressWeight = ModList.Count * 1.5}) '每个 Mod 需要 1.5s
            InstallLoaders.Add(New LoaderTask(Of Integer, Integer)("替换旧版资源文件",
            Sub()
                Try
                    For Each Entry As LocalCompFile In ModList
                        If File.Exists(Entry.Path) Then
                            My.Computer.FileSystem.DeleteFile(Entry.Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                        Else
                            Log($"[CompUpdate] 未找到更新前的资源文件，跳过对它的删除：{Entry.Path}", LogLevel.Debug)
                        End If
                    Next
                    For Each Entry As KeyValuePair(Of String, String) In FileCopyList
                        If File.Exists(Entry.Value) Then
                            My.Computer.FileSystem.DeleteFile(Entry.Value, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                            Log($"[Mod] 更新后的资源文件已存在，将会把它放入回收站：{Entry.Value}", LogLevel.Debug)
                        End If
                        If Directory.Exists(GetPathFromFullPath(Entry.Value)) Then
                            File.Move(Entry.Key, Entry.Value)
                            FinishedFileNames.Add(GetFileNameFromPath(Entry.Value))
                        Else
                            Log($"[Mod] 更新后的目标文件夹已被删除：{Entry.Value}", LogLevel.Debug)
                        End If
                    Next
                Catch ex As OperationCanceledException
                    Log(ex, "替换旧版资源文件时被主动取消")
                End Try
            End Sub))
            '结束处理
            Dim Loader As New LoaderCombo(Of IEnumerable(Of LocalCompFile))("资源更新：" & PageVersionLeft.Version.Name, InstallLoaders)
            Dim PathMods As String = PageVersionLeft.Version.PathIndie & If(PageVersionLeft.Version.Version.HasLabyMod, "labymod-neo\fabric\" & PageVersionLeft.Version.Version.McName & "\", "") & GetPathNameByCompType(CurrentCompType) & "\"
            Loader.OnStateChanged =
            Sub()
                '结果提示
                Select Case Loader.State
                    Case LoadState.Finished
                        Select Case FinishedFileNames.Count
                            Case 0 '一般是由于 Mod 文件被占用，然后玩家主动取消
                                Log($"[CompUpdate] 没有资源被成功更新")
                            Case 1
                                Hint($"已成功更新 {FinishedFileNames.Single}！", HintType.Finish)
                            Case Else
                                Hint($"已成功更新 {FinishedFileNames.Count} 个资源！", HintType.Finish)
                        End Select
                    Case LoadState.Failed
                        Hint("资源更新失败：" & GetExceptionSummary(Loader.Error), HintType.Critical)
                    Case LoadState.Aborted
                        Hint("资源更新已中止！", HintType.Info)
                    Case Else
                        Return
                End Select
                Log($"[CompUpdate] 已从正在进行资源更新的文件夹列表移除：{PathMods}")
                UpdatingVersions.Remove(PathMods)
                '清理缓存
                RunInNewThread(
                Sub()
                    Try
                        For Each TempFile In FileCopyList.Keys
                            If File.Exists(TempFile) Then File.Delete(TempFile)
                        Next
                    Catch ex As Exception
                        Log(ex, "清理资源更新缓存失败")
                    End Try
                End Sub, "Clean Comp Update Cache", ThreadPriority.BelowNormal)
            End Sub
            '启动加载器
            Log($"[CompUpdate] 开始更新 {ModList.Count} 个资源：{PathMods}")
            UpdatingVersions.Add(PathMods)
            Loader.Start()
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            ReloadCompFileList(True)
        Catch ex As Exception
            Log(ex, "初始化资源更新失败")
        End Try
    End Sub

    '删除
    Private Sub BtnSelectDelete_Click() Handles BtnSelectDelete.Click
        DeleteMods(CompResourceListLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName)))
        ChangeAllSelected(False)
    End Sub
    Private Sub DeleteMods(ModList As IEnumerable(Of LocalCompFile))
        Try
            Dim IsSuccessful As Boolean = True
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown
            '确认需要删除的文件
            ModList = ModList.SelectMany(
            Function(Target As LocalCompFile)
                If Target.IsFolder Then
                    ' 文件夹只需要删除自身
                    Return {Target.Path}
                ElseIf Target.State = LocalCompFile.LocalFileStatus.Fine Then
                    Return {Target.Path, Target.Path & If(File.Exists(Target.Path & ".old"), ".old", ".disabled")}
                Else
                    Return {Target.Path, Target.RawPath}
                End If
            End Function).Distinct.Where(Function(m) If(m.EndsWithF("\__FOLDER__", True), Directory.Exists(m.Replace("\__FOLDER__", "")), File.Exists(m))).Select(Function(m) New LocalCompFile(m)).ToList()
            '实际删除文件
            For Each ModEntity In ModList
                '删除
                Try
                    If ModEntity.IsFolder Then
                        ' 删除文件夹
                        If IsShiftPressed Then
                            Directory.Delete(ModEntity.ActualPath, True)
                        Else
                            My.Computer.FileSystem.DeleteDirectory(ModEntity.ActualPath, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                        End If
                    Else
                        ' 删除文件
                        If IsShiftPressed Then
                            File.Delete(ModEntity.Path)
                        Else
                            My.Computer.FileSystem.DeleteFile(ModEntity.Path, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                        End If
                    End If
                Catch ex As OperationCanceledException
                    Log(ex, "删除资源被主动取消")
                    ReloadCompFileList(True)
                    Return
                Catch ex As Exception
                    Log(ex, $"删除资源失败（{ModEntity.Path}）", LogLevel.Msgbox)
                    IsSuccessful = False
                End Try
                '取消选中
                SelectedMods.Remove(ModEntity.RawFileName)
                '更改 Loader 和 UI 中的列表
                CompResourceListLoader.Output.Remove(ModEntity)
                SearchResult?.Remove(ModEntity)
                ModItems.Remove(ModEntity.RawFileName)
                Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalCompItem).FirstOrDefault(Function(i) i.Entry.Equals(ModEntity)))
                If IndexOfUi >= 0 Then PanList.Children.RemoveAt(IndexOfUi)
            Next
            RefreshBars()
            If Not IsSuccessful Then
                Hint("由于文件被占用，删除失败，请尝试关闭正在运行的游戏后再试！", HintType.Critical)
                ReloadCompFileList(True)
            ElseIf PanList.Children.Count = 0 Then
                ReloadCompFileList(True) '删除了全部项目
            Else
                RefreshBars()
            End If
            '显示结果提示
            If Not IsSuccessful Then Return
            If IsShiftPressed Then
                If ModList.Count = 1 Then
                    Hint($"已彻底删除 {ModList.Single.FileName}！", HintType.Finish)
                Else
                    Hint($"已彻底删除 {ModList.Count} 个项目！", HintType.Finish)
                End If
            Else
                If ModList.Count = 1 Then
                    Hint($"已将 {ModList.Single.FileName} 删除到回收站！", HintType.Finish)
                Else
                    Hint($"已将 {ModList.Count} 个项目删除到回收站！", HintType.Finish)
                End If
            End If
        Catch ex As OperationCanceledException
            Log(ex, "删除资源被主动取消")
            ReloadCompFileList(True)
        Catch ex As Exception
            Log(ex, "删除资源出现未知错误", LogLevel.Feedback)
            ReloadCompFileList(True)
        End Try
        LoaderRun(LoaderFolderRunType.UpdateOnly)
    End Sub

    '取消选择
    Private Sub BtnSelectCancel_Click() Handles BtnSelectCancel.Click
        ChangeAllSelected(False)
    End Sub

    '收藏
    Private Sub BtnSelectFavorites_Click(sender As Object, e As RouteEventArgs) Handles BtnSelectFavorites.Click
        Dim Selected As List(Of CompProject) = CompResourceListLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName) AndAlso m.Comp IsNot Nothing).Select(Function(i) i.Comp).ToList
        CompFavorites.ShowMenu(Selected, sender)
    End Sub

    '分享
    Private Sub BtnSelectShare_Click() Handles BtnSelectShare.Click
        Dim ShareList As List(Of String) = CompResourceListLoader.Output.Where(Function(m) SelectedMods.Contains(m.RawFileName) AndAlso m.Comp IsNot Nothing).Select(Function(i) i.Comp.Id).ToList()
        ClipboardSet(CompFavorites.GetShareCode(ShareList))
        ChangeAllSelected(False)
    End Sub

#End Region

#Region "单个资源项"

    '详情
    Public Sub Info_Click(sender As Object, e As EventArgs)
        Try

            Dim ModEntry As LocalCompFile = CType(If(TypeOf sender Is MyIconButton, sender.Tag, sender), MyLocalCompItem).Entry
            '判断该 LabyMod 是否支持安装 Fabric Mod
            Dim ModdedLabyMod = PageVersionLeft.Version.Version.HasLabyMod AndAlso PageVersionLeft.Version.Modable
            '加载失败信息
            If ModEntry.State = LocalCompFile.LocalFileStatus.Unavailable Then
                MyMsgBox("无法读取此资源的信息。" & vbCrLf & vbCrLf & "详细的错误信息：" & GetExceptionDetail(ModEntry.FileUnavailableReason), "资源读取失败")
                Return
            End If
            If ModEntry.Comp IsNot Nothing Then
                '跳转到 Mod 下载页面
                FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail,
                    .Additional = {ModEntry.Comp, New List(Of String), PageVersionLeft.Version.Version.McName,
                        If(PageVersionLeft.Version.Version.HasForge, CompLoaderType.Forge,
                        If(PageVersionLeft.Version.Version.HasNeoForge, CompLoaderType.NeoForge,
                        If(PageVersionLeft.Version.Version.HasFabric OrElse ModdedLabyMod, CompLoaderType.Fabric, CompLoaderType.Any))), CurrentCompType}})
            Else
                '获取信息
                Dim ContentLines As New List(Of String)
                
                '检查是否为文件夹
                If ModEntry.IsFolder Then
                    '处理文件夹详情
                    Dim folderPath As String = ModEntry.ActualPath
                    If Directory.Exists(folderPath) Then
                        Dim fileCount As Integer = 0
                        Try
                            '根据当前资源类型计算文件数量
                            Select Case CurrentCompType
                                Case CompType.Schematic
                                    fileCount = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.AllDirectories).Where(Function(f) LocalCompFile.IsCompFile(f.FullName, CompType.Schematic)).Count()
                                Case CompType.Mod
                                    fileCount = New DirectoryInfo(folderPath).EnumerateFiles("*.jar", SearchOption.AllDirectories).Count()
                                Case CompType.ResourcePack
                                    fileCount = New DirectoryInfo(folderPath).EnumerateFiles("*.zip", SearchOption.AllDirectories).Count()
                                Case CompType.Shader
                                    fileCount = New DirectoryInfo(folderPath).EnumerateFiles("*.zip", SearchOption.AllDirectories).Count()
                                Case Else
                                    fileCount = New DirectoryInfo(folderPath).EnumerateFiles("*", SearchOption.AllDirectories).Count()
                            End Select
                        Catch ex As Exception
                            fileCount = 0
                        End Try
                        
                        If fileCount = 0 Then
                            ContentLines.Add("空文件夹" & vbCrLf)
                        ElseIf fileCount = 1 Then
                            ContentLines.Add("包含 1 个文件" & vbCrLf)
                        Else
                            ContentLines.Add($"包含 {fileCount} 个文件" & vbCrLf)
                        End If
                    Else
                        ContentLines.Add("文件夹不存在" & vbCrLf)
                    End If
                    ContentLines.Add("路径：" & folderPath)
                Else
                    '处理普通文件详情
                    If ModEntry.Description IsNot Nothing Then ContentLines.Add(ModEntry.Description & vbCrLf)
                    If ModEntry.Authors IsNot Nothing Then ContentLines.Add("作者：" & ModEntry.Authors)
                    ContentLines.Add("文件：" & ModEntry.FileName & "（" & GetString(New FileInfo(ModEntry.Path).Length) & "）")
                    If ModEntry.Version IsNot Nothing Then ContentLines.Add("版本：" & ModEntry.Version)
                    
                    '对于 .litematic 文件，显示额外的 NBT 数据
                    If ModEntry.Path.EndsWithF(".litematic", True) Then
                        ContentLines.Add("")
                        ContentLines.Add("详细信息：")
                        
                        If ModEntry.LitematicEnclosingSize IsNot Nothing Then
                            ContentLines.Add("大小：" & ModEntry.LitematicEnclosingSize)
                        End If
                        
                        If ModEntry.LitematicTotalBlocks.HasValue Then
                            ContentLines.Add("总方块数：" & ModEntry.LitematicTotalBlocks.Value.ToString("N0"))
                        End If
                        
                        If ModEntry.LitematicTotalVolume.HasValue Then
                            ContentLines.Add("总体积：" & ModEntry.LitematicTotalVolume.Value.ToString("N0"))
                        End If
                        
                        If ModEntry.LitematicRegionCount.HasValue Then
                            ContentLines.Add("区域数量：" & ModEntry.LitematicRegionCount.Value)
                        End If
                        
                        If ModEntry.LitematicTimeCreated.HasValue Then
                            Try
                                Dim createdTime As DateTime = DateTimeOffset.FromUnixTimeMilliseconds(ModEntry.LitematicTimeCreated.Value).DateTime
                                ContentLines.Add("创建时间：" & createdTime.ToString("yyyy-MM-dd HH:mm:ss"))
                            Catch
                                ' 如果时间转换失败，显示原始时间戳
                                ContentLines.Add("创建时间：" & ModEntry.LitematicTimeCreated.Value)
                            End Try
                        End If
                        
                        If ModEntry.LitematicTimeModified.HasValue Then
                            Try
                                Dim modifiedTime As DateTime = DateTimeOffset.FromUnixTimeMilliseconds(ModEntry.LitematicTimeModified.Value).DateTime
                                ContentLines.Add("修改时间：" & modifiedTime.ToString("yyyy-MM-dd HH:mm:ss"))
                            Catch
                                ' 如果时间转换失败，显示原始时间戳
                                ContentLines.Add("修改时间：" & ModEntry.LitematicTimeModified.Value)
                            End Try
                        End If
                    End If
                End If
                
                '只有普通文件才显示调试信息
                If Not ModEntry.IsFolder Then
                    Dim DebugInfo As New List(Of String)
                    If ModEntry.ModId IsNot Nothing Then
                        DebugInfo.Add("Mod ID：" & ModEntry.ModId)
                    End If
                    If ModEntry.Dependencies.Any Then
                        DebugInfo.Add("依赖于：")
                        For Each Dep In ModEntry.Dependencies
                            DebugInfo.Add(" - " & Dep.Key & If(Dep.Value Is Nothing, "", "，版本：" & Dep.Value))
                        Next
                    End If
                    If DebugInfo.Any Then
                        ContentLines.Add("")
                        ContentLines.AddRange(DebugInfo)
                    End If
                End If
                
                '显示详情信息
                If ModEntry.IsFolder Then
                    '文件夹只显示基本信息，不提供搜索功能
                    MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "返回")
                Else
                    '获取用于搜索的 Mod 名称
                    Dim ModOriginalName As String = ModEntry.Name.Replace(" ", "+")
                    Dim ModSearchName As String = ModOriginalName.Substring(0, 1)
                    For i = 1 To ModOriginalName.Count - 1
                        Dim IsLastLower As Boolean = ModOriginalName(i - 1).ToString.ToLower.Equals(ModOriginalName(i - 1).ToString)
                        Dim IsCurrentLower As Boolean = ModOriginalName(i).ToString.ToLower.Equals(ModOriginalName(i).ToString)
                        If IsLastLower AndAlso Not IsCurrentLower Then
                            '上一个字母为小写，这一个字母为大写
                            ModSearchName += "+"
                        End If
                        ModSearchName += ModOriginalName(i)
                    Next
                    ModSearchName = ModSearchName.Replace("++", "+").Replace("pti+Fine", "ptiFine")
                    '显示
                    If CurrentCompType = CompType.Schematic Then
                        '投影原理图文件不显示百科搜索选项
                        If ModEntry.Url Is Nothing Then
                            MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "返回")
                        Else
                            If MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "打开官网", "返回") = 1 Then
                                OpenWebsite(ModEntry.Url)
                            End If
                        End If
                    Else
                        '其他资源类型保留百科搜索功能
                        If ModEntry.Url Is Nothing Then
                            If MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "百科搜索", "返回") = 1 Then
                                OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                            End If
                        Else
                            Select Case MyMsgBox(Join(ContentLines, vbCrLf), ModEntry.Name, "打开官网", "百科搜索", "返回")
                                Case 1
                                    OpenWebsite(ModEntry.Url)
                                Case 2
                                    OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                            End Select
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Log(ex, "获取资源详情失败", LogLevel.Feedback)
        End Try
    End Sub
    '打开文件所在的位置
    Public Sub Open_Click(sender As MyIconButton, e As EventArgs)
        Try
            Dim ListItem As MyLocalCompItem = sender.Tag
            ' 对于文件夹使用实际路径，对于文件使用原路径
            Dim targetPath As String = If(ListItem.Entry.IsFolder, ListItem.Entry.ActualPath, ListItem.Entry.Path)
            OpenExplorer(targetPath)
        Catch ex As Exception
            Log(ex, "打开资源文件位置失败", LogLevel.Feedback)
        End Try
    End Sub
    '删除
    Public Sub Delete_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalCompItem = sender.Tag
        DeleteMods({ListItem.Entry})
    End Sub
    '启用 / 禁用
    Public Sub ED_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalCompItem = sender.Tag
        EDMods({ListItem.Entry}, ListItem.Entry.State = LocalCompFile.LocalFileStatus.Disabled)
    End Sub

#End Region

#Region "搜索"

    Public ReadOnly Property IsSearching As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(SearchBox.Text)
        End Get
    End Property
    Private SearchResult As List(Of LocalCompFile)

    Public Sub SearchRun() Handles SearchBox.TextChanged
        If IsSearching Then
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of LocalCompFile))
            For Each Entry As LocalCompFile In CompResourceListLoader.Output
                Dim SearchSource As New List(Of KeyValuePair(Of String, Double))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Name, 1))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.FileName, 1))
                If Entry.Version IsNot Nothing Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Version, 0.2))
                End If
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Description, 0.4))
                End If
                If Entry.Comp IsNot Nothing Then
                    If Entry.Comp.RawName <> Entry.Name Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.RawName, 1))
                    If Entry.Comp.TranslatedName <> Entry.Comp.RawName Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.TranslatedName, 1))
                    If Entry.Comp.Description <> Entry.Description Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Comp.Description, 0.4))
                    SearchSource.Add(New KeyValuePair(Of String, Double)(String.Join("", Entry.Comp.Tags), 0.2))
                End If
                QueryList.Add(New SearchEntry(Of LocalCompFile) With {.Item = Entry, .SearchSource = SearchSource})
            Next
            '进行搜索
            SearchResult = Search(QueryList, SearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35).Select(Function(r) r.Item).ToList
        End If
        RefreshUI()
    End Sub

#End Region

End Class
