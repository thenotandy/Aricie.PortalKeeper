Imports Aricie.Services
Imports System.Reflection
Imports DotNetNuke.Services.FileSystem
Imports DotNetNuke.Common.Utilities
Imports System.IO

Namespace Services
    Public Class ObsoleteDNNProvider

        ' singleton reference to the instantiated object 
        Private Shared objProvider As ObsoleteDNNProvider = Nothing

        ' constructor
        Shared Sub New()
            CreateProvider()
        End Sub

        Public Shared Function ResolveEmbeddedAssembly(senders As Object, args As ResolveEventArgs) As Assembly
            Dim resourceName As [String] = New AssemblyName(args.Name).Name + ".dll"
            For Each realResourceName As String In Assembly.GetExecutingAssembly().GetManifestResourceNames()
                If realResourceName.Contains(resourceName) Then
                    Using stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(realResourceName)
                        Dim assemblyData As [Byte]() = New [Byte](CInt(stream.Length - 1)) {}
                        stream.Read(assemblyData, 0, assemblyData.Length)
                        Return Assembly.Load(assemblyData)
                    End Using
                End If
            Next
            Return Nothing
        End Function


        ' dynamically create provider
        Private Shared Sub CreateProvider()

            If NukeHelper.DnnVersion.Major < 7 Then
                objProvider = New ObsoleteDNNProvider()
            Else
                Try

                    AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf ResolveEmbeddedAssembly

                    'Function(sender, args)
                    'Dim resourceName As [String] = New AssemblyName(args.Name).Name + ".dll"
                    'For Each realResourceName As String In Assembly.GetExecutingAssembly().GetManifestResourceNames()
                    '    If realResourceName.Contains(resourceName) Then
                    '        Using stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                    '            Dim assemblyData As [Byte]() = New [Byte](CInt(stream.Length - 1)) {}
                    '            stream.Read(assemblyData, 0, assemblyData.Length)
                    '            Return Assembly.Load(assemblyData)
                    '        End Using
                    '    End If
                    'Next
                    'Return Nothing
                    '                                                    End Function


                    'Dim objAssembly As Assembly = Assembly.LoadFile("Aricie.DNN7.dll")
                    '(NukeHelper.GetModuleDirectoryMapPath("Aricie-Shared").TrimEnd("\"c) & "\DNN7\Aricie.DNN7.dll")
                    objProvider = CType(ReflectionHelper.CreateObject("Aricie.DNN.DNN7ObsoleteDNNProvider, Aricie.DNN7"), ObsoleteDNNProvider)

                    RemoveHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf ResolveEmbeddedAssembly

                Catch ex As Exception
                    ExceptionHelper.LogException(ex)
                    objProvider = New ObsoleteDNNProvider()
                End Try
            End If
        End Sub

        ' return the provider
        Public Shared ReadOnly Property Instance() As ObsoleteDNNProvider
            Get
                Return objProvider
            End Get
        End Property

        Public Overridable Sub AddFolder(objFolderInfo As FolderInfo)
            NukeHelper.FolderController.AddFolder(objFolderInfo.PortalID, objFolderInfo.FolderPath, objFolderInfo.StorageLocation, objFolderInfo.IsProtected, objFolderInfo.IsCached)
        End Sub

        Public Overridable Function AddOrUpdateFile(objFile As DotNetNuke.Services.FileSystem.FileInfo, content As Byte()) As Integer

            If NukeHelper.DnnVersion.Major < 6 Then
                Dim fileId As Integer = objFile.FileId
                If fileId <= 0 Then
                    fileId = NukeHelper.FileController.AddFile(objFile)
                End If
                If fileId > 0 Then
                    NukeHelper.FileController.UpdateFileContent(fileId, content)
                End If
                Return fileId
            Else
                Dim params As Object() = {objFile.FolderId}
                Dim objFolder As FolderInfo = DirectCast(FolderManagerGetFolderMethod.Invoke(FolderManagerInstance, params), FolderInfo)
                If objFolder IsNot Nothing Then
                    Using objStream As New MemoryStream(content)
                        If objFile.FileId > 0 Then
                            params = {objFile, objStream}
                            Return DirectCast(FileManagerUpdateFileMethod.Invoke(FileManagerInstance, params), DotNetNuke.Services.FileSystem.FileInfo).FileId
                        Else
                            params = {objFolder, objFile.FileName, objStream}
                            Return DirectCast(FileManagerAddFileMethod.Invoke(FileManagerInstance, params), DotNetNuke.Services.FileSystem.FileInfo).FileId
                        End If
                    End Using
                End If
            End If
            Return -1
        End Function


       





        Public Overridable Function GetFolderFromPath(portalId As Integer, path As String) As FolderInfo
            Return NukeHelper.FolderController.GetFolder(portalId, path, False)
        End Function

        Public Overridable Function GetFileContent(objFileInfo As DotNetNuke.Services.FileSystem.FileInfo) As Byte()
            If NukeHelper.DnnVersion.Major < 6 Then
                Return NukeHelper.FileController.GetFileContent(objFileInfo.FileId, objFileInfo.PortalId)
            Else
                Dim fileManagerType As Type = FileManagerInstance.GetType()
                Dim getFileContentMethod As MethodInfo = fileManagerType.GetMethod("GetFileContent")
                Dim params As Object() = {objFileInfo}
                Dim res As Stream = DirectCast(getFileContentMethod.Invoke(FileManagerInstance, params), Stream)
                Dim tr As New System.IO.StreamReader(res, System.Text.Encoding.UTF8)

                Dim str = tr.ReadToEnd()


                'Dim streamLength As Integer = Convert.ToInt32(res.Length)
                '                Dim fileData() As Byte = New Byte(streamLength) {}

                'res.Read(fileData, 0, streamLength)
                'res.Close()

                Return System.Text.Encoding.UTF8.GetBytes(str)

            End If



        End Function


        Public Overridable Sub ClearFileContent(objFileInfo As DotNetNuke.Services.FileSystem.FileInfo)

            NukeHelper.FileController.ClearFileContent(objFileInfo.FileId)

        End Sub


        Public Overridable Function GetFiles(folderInfo As FolderInfo) As IEnumerable(Of DotNetNuke.Services.FileSystem.FileInfo)
            Return CBO.FillCollection(Of DotNetNuke.Services.FileSystem.FileInfo)(NukeHelper.FileController.GetFiles(folderInfo.PortalID, folderInfo.FolderID))
        End Function

#Region "Private members"

        Private _FolderManagerInstance As Object = Nothing

        Private ReadOnly Property FolderManagerInstance As Object
            Get
                If _FolderManagerInstance Is Nothing Then
                    'Dim folderManagerType As Type = ReflectionHelper.CreateType("DotNetNuke.Services.FileSystem.FolderManager, DotNetNuke")
                    Dim componentBaseType As Type = ReflectionHelper.CreateType("DotNetNuke.ComponentModel.ComponentBase`2[[DotNetNuke.Services.FileSystem.IFolderManager, DotNetNuke],[DotNetNuke.Services.FileSystem.FolderManager, DotNetNuke]], DotNetNuke")
                    _FolderManagerInstance = componentBaseType.GetProperty("Instance").GetValue(Nothing, Nothing)
                End If
                Return _FolderManagerInstance
            End Get
        End Property

        Private _FolderManagerGetFolderMethod As MethodInfo

        Public ReadOnly Property FolderManagerGetFolderMethod As MethodInfo
            Get
                If _FolderManagerGetFolderMethod Is Nothing Then
                    Dim folderManagerType As Type = FolderManagerInstance.GetType()
                    For Each objMethod As MethodInfo In folderManagerType.GetMethods()
                        If objMethod.Name = "GetFolder" Then
                            Dim objParameters As ParameterInfo() = objMethod.GetParameters()
                            If objParameters.Count = 1 Then
                                If objParameters(0).ParameterType Is GetType(Integer) Then
                                    _FolderManagerGetFolderMethod = objMethod
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                End If
                Return _FolderManagerGetFolderMethod
            End Get
        End Property


        Private _FileManagerInstance As Object = Nothing


        Private ReadOnly Property FileManagerInstance As Object
            Get
                If _FileManagerInstance Is Nothing Then
                    'Dim fileManagerType As Type = ReflectionHelper.CreateType("DotNetNuke.Services.FileSystem.FileManager, DotNetNuke")
                    Dim componentBaseType As Type = ReflectionHelper.CreateType("DotNetNuke.ComponentModel.ComponentBase`2[[DotNetNuke.Services.FileSystem.IFileManager, DotNetNuke],[DotNetNuke.Services.FileSystem.FileManager, DotNetNuke]], DotNetNuke")
                    _FileManagerInstance = componentBaseType.GetProperty("Instance").GetValue(Nothing, Nothing)
                End If
                Return _FileManagerInstance
            End Get
        End Property

        Private _FileManagerUpdateFileMethod As MethodInfo

        Public ReadOnly Property FileManagerUpdateFileMethod As MethodInfo
            Get
                If _FileManagerUpdateFileMethod Is Nothing Then
                    Dim fileManagerType As Type = FileManagerInstance.GetType()
                    For Each objMethod As MethodInfo In fileManagerType.GetMethods()
                        If objMethod.Name = "UpdateFile" Then
                            Dim objParameters As ParameterInfo() = objMethod.GetParameters()
                            If objParameters.Count = 2 Then
                                If objParameters(1).ParameterType Is GetType(Stream) Then
                                    _FileManagerUpdateFileMethod = objMethod
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                End If
                Return _FileManagerUpdateFileMethod
            End Get
        End Property


        Private _FileManagerAddFileMethod As MethodInfo

        Private ReadOnly Property FileManagerAddFileMethod As MethodInfo
            Get
                If _FileManagerAddFileMethod Is Nothing Then
                    Dim fileManagerType As Type = FileManagerInstance.GetType()
                    For Each objMethod As MethodInfo In fileManagerType.GetMethods()
                        If objMethod.Name = "AddFile" Then
                            Dim objParameters As ParameterInfo() = objMethod.GetParameters()
                            If objParameters.Count = 3 Then
                                If objParameters(2).ParameterType Is GetType(Stream) Then
                                    _FileManagerAddFileMethod = objMethod
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                End If
                Return _FileManagerAddFileMethod
            End Get
        End Property

#End Region


    End Class
End Namespace