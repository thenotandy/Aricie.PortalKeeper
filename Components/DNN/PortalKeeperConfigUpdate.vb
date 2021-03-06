Imports Aricie.DNN.Configuration
'Imports OpenRasta.Hosting.AspNet
Imports Aricie.DNN.Services

Namespace Aricie.DNN.Modules.PortalKeeper


    Public Class PortalKeeperConfigUpdate
        Implements IUpdateProvider


        Public Function GetConfigElements() As List(Of IConfigElementInfo) Implements IUpdateProvider.GetConfigElements
            Dim toReturn As New List(Of IConfigElementInfo)
            toReturn.Add(New SchedulerTaskElementInfo("PortalKeeper Bot Farm", GetType(PortalKeeperSchedule), TimeSpan.FromMinutes(1)))
            Dim toAdd As WebServerElementInfo = New HttpModuleInfo("Aricie.PortalKeeper", GetType(PortalKeeperModule), "")
            If NukeHelper.DnnVersion.Major > 6 Then
                toAdd.InsertBeforeKey = "RequestFilter"
            Else
                toAdd.InsertBeforeKey = "UrlRewrite"
            End If

            toReturn.Add(toAdd)

            toAdd = New HttpModuleInfo("Aricie.PortalKeeperAfterDNN", GetType(PortalKeeperModuleAfterDNN), "")
            toAdd.InsertAfterKey = "UrlRewrite"
            toReturn.Add(toAdd)

            ''Jesse:      OpenRasta()
            'todo: g�rer les services web
            'toAdd = New HttpModuleInfo("OpenRastaModule", GetType(OpenRastaModule), "managedHandler")
            'toReturn.Add(toAdd)
            'toAdd = New HttpHandlerInfo("OpenRastaHandler", GetType(OpenRastaHandler), "*.rastahook", "*", "integratedMode")
            'toReturn.Add(toAdd)

            toReturn.Add(New TrustInfo("Full"))


            Return toReturn
        End Function


    End Class

    Public Class PortalKeeperRastaConfigUpdate
        Implements IUpdateProvider



        Public Function GetConfigElements() As System.Collections.Generic.List(Of IConfigElementInfo) Implements IUpdateProvider.GetConfigElements
            Dim toReturn As New List(Of IConfigElementInfo)

            'Dim toAdd As WebServerElementInfo
            'Jesse:      OpenRasta()
            'toAdd = New HttpModuleInfo("OpenRastaModule", GetType(OpenRastaModule), "managedHandler")
            'toReturn.Add(toAdd)
            'toAdd = New HttpHandlerInfo("OpenRastaHandler", GetType(OpenRastaHandler), "*.rastahook", "*", "integratedMode")
            'toReturn.Add(toAdd)

            Return toReturn
        End Function


    End Class


End Namespace
