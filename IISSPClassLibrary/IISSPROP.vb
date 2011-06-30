Imports System.Xml
Imports System.Net
Imports System.IO
Imports System.IO.Stream
Imports System.Security.Cryptography.X509Certificates
Imports IISSPClassLibrary.IISSPGeneral
Imports IISSPClassLibrary.IISSPInbox

<ComClass(IISSPROP.ClassId, IISSPROP.InterfaceId, IISSPROP.EventsId)> _
Public Class IISSPROP

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "4ebaa2c4-e9f7-4b8d-b72a-e866b7d88f16"
    Public Const InterfaceId As String = "1fe3708f-85d9-41a1-ae35-3a597895cb1d"
    Public Const EventsId As String = "310212b0-1b65-415d-bb6c-4588374baf0f"
#End Region

    Private _storageName As String

    Private _General As IISSPGeneral
    Dim doc As Object

    ''' <summary>
    ''' Instance třídy <see cref="IISSPGeneral"></see>. Zajišťuje vlastní přenos dotazů.
    ''' Pomocí vlastností tohoto objektu nastavujeme parametry vlastního přenosu. 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property General() As IISSPGeneral
        Set(ByVal value As IISSPGeneral)
            _General = value
        End Set
        Get
            Return _General
        End Get
    End Property

    Public Sub New()
        MyBase.New()
        _General = New IISSPGeneral
    End Sub

    ''' <summary>
    ''' Pokud vytvoříme instanci objektu pomocí tohoto konstruktoru, budou její vlastnosti
    ''' automaticky nastaveny z naposledy uložených hodnot dle parametru <paramref name="Name"></paramref>. 
    ''' Tyto hodnoty se do standartního umístění uloží vždy před deaktivací objektu.
    ''' </summary>
    ''' <param name="Name">Název pod ktrým bude možné identifikovat položku v nastavení</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal Name As String)
        MyBase.New()
    End Sub
    '''<summary>
    '''Generuje dotaz na RISRE pro zaslání požadavku na rozpočtové opatření) viz. IISSP - Integrace s modulem IISSP RIS Realizace rozpočtu kapitola 3.2.1
    ''' vstupní parametr - vlastní XML dotaz (bez 1. řádky ?xml version = "1.0" encoding="UTF-8"?)
    '''</summary>
    Public Function GetEKIS_SP_ROP(ByVal XMLmsg As String) As String

        Dim doc As XmlDocument = New XmlDocument
        General.Log("Volám: GetEKIS_SP_ROP", Me)
        doc.LoadXml(XMLmsg)

        'Dim nsmgrR As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        'nsmgrR.AddNamespace("risre", "urn:cz:mfcr:iissp:schemas:Risre:v1")

        Dim ib As IISSPInbox = New IISSPInbox

        ' nacteme soap obalku
        Dim SEXml As XmlDocument = New XmlDocument

        ' nacitame z resource - az bude funkcni wsdl budeme cist z neho
        SEXml.Load(General.WorkingDirectory & "Settings\SoapEnvelope.xml")

        ' zaregistrujem si namespace
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(SEXml.NameTable)
        nsmgr.AddNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")

        ' pripravime telo Soap dotazu
        Dim SoapNode As XmlNode = SEXml.SelectSingleNode("/SOAP-ENV:Envelope/SOAP-ENV:Body", nsmgr)

        ' a zpravu vlozime do Soap obalky
        SoapNode.AppendChild(SEXml.ImportNode(doc.DocumentElement, True))

        ' a vratime cely xml dokument - kompletni dotaz na inbox
        'doc = ib.SendRequest(General.Url_EKIS_SP_ROP, "", "", SEXml)
        'General.MyRequest = SEXml.InnerXml
        General.MyRequest = SEXml.InnerXml
        General.Url = General.Url_EKIS_SP_ROP

        Return General.Request()

    End Function

    '''<summary>
    '''Generuje dotaz na RISRE pro získání Rozpočtových kmenových dat (čísleníků) viz. IISSP - Integrace s modulem IISSP RIS Realizace rozpočtu kapitola 4
    '''</summary>

    Public Function GetFMMD_CISELNIK(ByVal Ciselnik As String, Optional ByVal Kapitola As String = "", Optional ByVal DatumPlatnostiOd As String = "", Optional ByVal DatumPlatnostiDo As String = "", _
    Optional ByVal RozpoctovyRozsahZahrnutiVyrazeniKod As String = "", Optional ByVal RozpoctovyRozsahIntervalKod As String = "", Optional ByVal RozpoctovyRozsahRozsahOd As String = "", Optional ByVal RozpoctovyRozsahRozsahDo As String = "", _
    Optional ByVal DatumCasUdrzbaPosledniRozsahZahrnutiVyrazeniKod As String = "", Optional ByVal DatumCasUdrzbaPosledniRozsahIntervalKod As String = "", Optional ByVal DatumCasUdrzbaPosledniRozsahDatumCasRozsahOd As String = "", Optional ByVal DatumCasUdrzbaPosledniRozsahDatumCasRozsahDo As String = "", Optional ByVal RokFiskalni As String = "") As String

        If General.ClientCertificate Is Nothing Then
            General.SetClientCertificate(General.ClientCertificatePath, General.ClientCertificatePWD)
        End If

        ' Nacist do XMLDocument
        Dim doc As XmlDocument = New XmlDocument

        doc.Load(General.WorkingDirectory & "Settings\RISRE.xml")
        doc.InnerXml = doc.DocumentElement.OuterXml.Replace("ciselnik", Ciselnik)

        'Create an XmlNamespaceManager for resolving namespaces.
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        nsmgr.AddNamespace("risre", "urn:cz:mfcr:iissp:schemas:Risre:v1")

        If Kapitola <> "" Then
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).InnerText = Kapitola
        Else
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:Kapitola", nsmgr))
        End If

        'RozpocetnictviOrientovaneCilove
        If RokFiskalni <> "" Then
            doc.SelectSingleNode("//risre:RokFiskalni", nsmgr).InnerText = RokFiskalni
        Else
            doc.SelectSingleNode("//risre:RokFiskalni", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:RokFiskalni", nsmgr))
        End If

        If DatumPlatnostiOd <> "" Then
            doc.SelectSingleNode("//risre:DatumPlatnostiOd", nsmgr).InnerText = DatumPlatnostiOd
        Else
            doc.SelectSingleNode("//risre:DatumPlatnostiOd", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumPlatnostiOd", nsmgr))
        End If

        If DatumPlatnostiDo <> "" Then
            doc.SelectSingleNode("//risre:DatumPlatnostiDo", nsmgr).InnerText = DatumPlatnostiDo
        Else
            doc.SelectSingleNode("//risre:DatumPlatnostiDo", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumPlatnostiDo", nsmgr))
        End If

        '#########################################################################################################

        If RozpoctovyRozsahZahrnutiVyrazeniKod <> "" Then
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:ZahrnutiVyrazeniKod", nsmgr).InnerText = RozpoctovyRozsahZahrnutiVyrazeniKod
        Else
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:ZahrnutiVyrazeniKod", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:ZahrnutiVyrazeniKod", nsmgr))
        End If

        If RozpoctovyRozsahIntervalKod <> "" Then
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:IntervalKod", nsmgr).InnerText = RozpoctovyRozsahIntervalKod
        Else
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:IntervalKod", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:IntervalKod", nsmgr))
        End If

        If RozpoctovyRozsahRozsahOd <> "" Then
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahOd", nsmgr).InnerText = RozpoctovyRozsahRozsahOd
        Else
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahOd", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahOd", nsmgr))
        End If

        If RozpoctovyRozsahRozsahDo <> "" Then
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahDo", nsmgr).InnerText = RozpoctovyRozsahRozsahDo
        Else
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahDo", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah/risre:RozsahDo", nsmgr))
        End If

        If DatumCasUdrzbaPosledniRozsahZahrnutiVyrazeniKod <> "" Then
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:ZahrnutiVyrazeniKod", nsmgr).InnerText = DatumCasUdrzbaPosledniRozsahZahrnutiVyrazeniKod
        Else
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:ZahrnutiVyrazeniKod", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:ZahrnutiVyrazeniKod", nsmgr))
        End If

        If DatumCasUdrzbaPosledniRozsahIntervalKod <> "" Then
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:IntervalKod", nsmgr).InnerText = DatumCasUdrzbaPosledniRozsahIntervalKod
        Else
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:IntervalKod", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:IntervalKod", nsmgr))
        End If

        If DatumCasUdrzbaPosledniRozsahDatumCasRozsahOd <> "" Then
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahOd", nsmgr).InnerText = DatumCasUdrzbaPosledniRozsahDatumCasRozsahOd
        Else
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahOd", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahOd", nsmgr))
        End If

        If DatumCasUdrzbaPosledniRozsahDatumCasRozsahDo <> "" Then
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahDo", nsmgr).InnerText = DatumCasUdrzbaPosledniRozsahDatumCasRozsahDo
        Else
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahDo", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah/risre:DatumCasRozsahDo", nsmgr))
        End If

        'Odstranit Nodes bez child - muze nastat pri kombinaci dotazu

        Dim nodeRozsah As XmlNode = doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah", nsmgr)

        If doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah", nsmgr).HasChildNodes = False Then
            doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:" & Ciselnik & "Rozsah", nsmgr))
        End If

        If doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah", nsmgr).HasChildNodes = False Then
            doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:DatumCasUdrzbaPosledniRozsah", nsmgr))
        End If


        Dim ib As IISSPInbox = New IISSPInbox
        doc = ib.MakeRISRERequestXml(doc.DocumentElement)
        'doc = ib.SendRequest(General.Url_FMMD, "", "", doc)
        General.MyRequest = doc.InnerXml
        General.Url = General.Url_FMMD
        Return General.Request()

        'Return General.MyRequest


        'Return doc.InnerXml

    End Function
   
    '''<summary>
    '''Generuje dotaz na RISRE pro přenášení informací o ROP z IISSP RISRE do OOS(EKIS)  viz. IISSP - Integrace s modulem IISSP RIS Realizace rozpočtu kapitola 3.2.2
    '''</summary>
    Public Function GetSP_EKIS_ROP(Optional ByVal Kapitola As String = "", Optional ByVal RozpoctoveOpatreniRok As String = "", Optional ByVal RozpoctoveOpatreniCislo As String = "") As String

        ' Nacist do XMLDocument
        Dim doc As XmlDocument = New XmlDocument

        doc.Load(General.WorkingDirectory & "Settings\RISRE_EKIS_ROP.xml")

        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        nsmgr.AddNamespace("risre", "urn:cz:mfcr:iissp:schemas:Risre:v1")

        If Kapitola <> "" Then
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).InnerText = Kapitola
        Else
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:Kapitola", nsmgr))
        End If

        'RozpoctoveOpatreniRok
        If RozpoctoveOpatreniRok <> "" Then
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr).InnerText = RozpoctoveOpatreniRok
        Else
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr))
        End If

        'RozpoctoveOpatreniRok
        If RozpoctoveOpatreniCislo <> "" Then
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr).InnerText = RozpoctoveOpatreniCislo
        Else
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr))
        End If

        Dim ib As IISSPInbox = New IISSPInbox
        doc = ib.MakeRISRERequestXml(doc.DocumentElement)
        'doc = ib.SendRequest(General.Url_SP_EKIS_ROP, "", "", doc)
        General.MyRequest = doc.InnerXml
        General.Url = General.Url_SP_EKIS_ROP
        Return General.Request()
        'Return doc.InnerXml
        'Return General.MyRequest
    End Function
    '''<summary>
    '''Generuje dotaz na RISRE pro zaslání přílohy k rozpočtové opatření) viz. IISSP - Integrace s modulem IISSP RIS Realizace rozpočtu kapitola 3.2.1.2
    ''' strFileName - pln8 cesta k souboru přílohy např. c:\Test\Priloha.txt
    ''' Velikost souboru je omezena 5 MB
    '''</summary>
    Public Function GetEKIS_SP_ROP_ATT(ByVal strFileName As String, ByVal Kapitola As String, ByVal RozpoctoveOpatreniRok As String, ByVal RozpoctoveOpatreniCislo As String) As String

        ' Nacist do XMLDocument
        Dim doc As XmlDocument = New XmlDocument

        doc.Load(General.WorkingDirectory & "Settings\RISRE_EKIS_SP_ROP_ATT.xml")

        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        nsmgr.AddNamespace("risre", "urn:cz:mfcr:iissp:schemas:Risre:v1")

        If Kapitola <> "" Then
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).InnerText = Kapitola
        Else
            doc.SelectSingleNode("//risre:Kapitola", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:Kapitola", nsmgr))
        End If

        'RozpoctoveOpatreniRok
        If RozpoctoveOpatreniRok <> "" Then
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr).InnerText = RozpoctoveOpatreniRok
        Else
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:RozpoctoveOpatreniRok", nsmgr))
        End If

        'RozpoctoveOpatreniRok
        If RozpoctoveOpatreniCislo <> "" Then
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr).InnerText = RozpoctoveOpatreniCislo
        Else
            doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr).ParentNode.RemoveChild(doc.SelectSingleNode("//risre:RozpoctoveOpatreniCislo", nsmgr))
        End If

        Dim ib As IISSPInbox = New IISSPInbox
        doc = ib.MakeRISRERequestXml(doc.DocumentElement)

        'doc = ib.SendRequest(General.Url_SP_EKIS_ROP, "", "", doc)
        General.MyRequest = doc.InnerXml
        General.Url = General.Url_EKIS_SP_ROP_ATT
        'Return General.Request_ATT(strFileName)
        'Return doc.InnerXml
        'Return General.MyRequest
    End Function
End Class


