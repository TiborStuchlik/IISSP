Imports System.Xml
Imports System.Net
Imports System.IO
Imports System.IO.Stream
Imports IISSPClassLibrary
Imports System.Security.Cryptography.X509Certificates


''' <summary>
''' Tato třída obsluhuje Inboxy IISSP. A to CSUIS Inbox a RIS Inbox. Pomocí funkce <see cref="IISSPInbox.GetMessagesHeaders"></see>
''' , stáhne hlavičky zpráv a pomocí funkce <see cref="IISSPInbox.GetMessageById"></see> stáhne tělo zprávy. IISSPInbox
''' disponuje třídou <see cref="IISSPGeneral">General</see> zajistí samotný dotaz s předem nadefinovanými parametry přenosu.
''' </summary>
''' <remarks>V současné době zajišťuje také žádosti o číselníky pomocí funkce <see cref="IISSPInbox.MakeRISRERequestXml"></see>
''' . Tato funkce bude v budoucnu přesunuta pod třídu zajišťující podobné funkce. Sledujte "Co je nového"</remarks>
<ComClass(IISSPInbox.ClassId, IISSPInbox.InterfaceId, IISSPInbox.EventsId)> _
Public Class IISSPInbox

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "a79b7af8-5922-4596-91a9-c21d48ec78db"
    Public Const InterfaceId As String = "d0bfcb9d-0681-470b-bc1e-d811a9bff337"
    Public Const EventsId As String = "9b5a0d88-5729-45e4-8a6a-e5411e51c2a2"
#End Region

    ' A creatable COM class must have a Public Sub New() 
    ' with no parameters, otherwise, the class will not be 
    ' registered in the COM registry and cannot be created 
    ' via CreateObject.

    Private _storageName As String

    Private _General As IISSPGeneral
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
    ''' <param name="Name">Název pod kterým bude možné identifikovat položku v nastavení</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal Name As String)
        MyBase.New()
    End Sub

    ''' <summary>
    ''' Vytvoření interního hlášení
    ''' </summary>
    ''' <param name="number">Interní číslo hlášení</param>
    ''' <param name="name">Interní název hlášení</param>
    ''' <param name="popis">Převezme systémovou, nebo extrní vyjímku.</param>
    ''' <returns>XmlDocument, kde je root element <c>error</c></returns>
    ''' <remarks>Doporučujeme používat ve všech částech komponenty pro jednotná hlášení</remarks>
    Private Function MakeErrorAnswer(ByVal number As Integer, ByVal name As String, ByVal popis As String)
        Dim ErrXml As XmlDocument = New XmlDocument
        ErrXml.Load(General.WorkingDirectory & "Settings\ErrorRequest.xml")
        ErrXml.SelectSingleNode("//Popis").InnerText = popis
        ErrXml.SelectSingleNode("//Nazev").InnerText = name
        ErrXml.SelectSingleNode("//Number").InnerText = number.ToString
        General.Log(ErrXml.InnerXml, Me)
        Return ErrXml
    End Function

    ''' <summary>
    ''' zde vytvorime kompletni XmlDokument s pozadavekem na RISRE
    ''' </summary>
    ''' <param name="InboxRequestElement">Element, který bude vložen do těla dotazu.</param>
    ''' <returns>XmlDokument s odpovědí</returns>
    ''' <remarks>Tato funkce bude později přesunuta</remarks>
    Public Function MakeRISRERequestXml(ByVal InboxRequestElement As XmlElement) As XmlDocument
        General.Log("Generuji dotaz RISRE", Me)
        Dim doc As XmlDocument = New XmlDocument
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        ' zaregistrujem si namespace
        nsmgr.AddNamespace("SOAP", "http://schemas.xmlsoap.org/soap/envelope/")
        nsmgr.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        nsmgr.AddNamespace("cmn", "urn:cz:mfcr:iissp:schemas:Common:v1")
        ' nacteme obalku dotazu pro Inbox
        Dim MEXml As XmlDocument = New XmlDocument
        MEXml.Load(General.WorkingDirectory & "Settings\RISREEnvelope.xml")
        'nastavime GUID
        'MEXml.SelectSingleNode("//cmn:TransactionId", nsmgr).InnerText = General.GetGuid()
        'nastavime datum
        MEXml.SelectSingleNode("//msg:DateTimeCreated", nsmgr).InnerText = General.GetDateTime()
        'vyberem body
        Dim MeXmlNode As XmlNode = MEXml.SelectSingleNode("//msg:EnvelopeBody", nsmgr)
        ' pridame namespace bez nej zahlasi server chybu 500
        ' MEXml.DocumentElement.SetAttribute("xmlns", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        ' vlozime vlastni dotaz zaslanej parametrem InboxRequstXml
        MeXmlNode.AppendChild(MEXml.ImportNode(InboxRequestElement, True))
        ' nacteme soap obalku
        Dim SEXml As XmlDocument = New XmlDocument
        ' nacitame z resource - az bude funkcni wsdl budeme cist z neho
        SEXml.Load(General.WorkingDirectory & "Settings\SoapEnvelope.xml")
        ' pripravime telo Soap dotazu
        Dim SoapNode As XmlNode = SEXml.SelectSingleNode("/SOAP:Envelope/SOAP:Body", nsmgr)
        ' a zpravu vlozime do Soap obalky
        SoapNode.AppendChild(SEXml.ImportNode(MEXml.DocumentElement, True))
        ' a vratime cely xml dokument - kompletni dotaz na inbox
        Return SEXml
    End Function

    ''' <summary>
    ''' Čeká na odstranění. Je nahrazena.
    ''' </summary>
    ''' <param name="InboxRequestElement"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function MakeInboxRequestXml(ByVal InboxRequestElement As XmlElement) As XmlDocument
        General.Log("Generuji dotaz " + General.RecipientModule + " INBOX", Me)
        ' nacteme obalku dotazu pro Inbox
        Dim MEXml As XmlDocument = New XmlDocument
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(MEXml.NameTable)

        nsmgr.AddNamespace("SOAP", "http://schemas.xmlsoap.org/soap/envelope/")
        nsmgr.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        nsmgr.AddNamespace("cmn", "urn:cz:mfcr:iissp:schemas:Common:v1")
        MEXml.Load(General.WorkingDirectory & "Settings\RisreCsuisEnvelopeLayout.xml")

        'nastavime GUID
        MEXml.SelectSingleNode("//cmn:TransactionId", nsmgr).InnerText = General.GetGuid()
        'nastavime datum
        MEXml.SelectSingleNode("//msg:DateTimeCreated", nsmgr).InnerText = General.GetDateTime()
        'vyberem body
        Dim MeXmlNode As XmlNode = MEXml.SelectSingleNode("//msg:EnvelopeBody", nsmgr)
        ' vlozime vlastni dotaz zaslanej parametrem InboxRequstXml
        MeXmlNode.AppendChild(MEXml.ImportNode(InboxRequestElement, True))
        ' nacteme soap obalku
        Dim SEXml As XmlDocument = New XmlDocument
        ' nacitame z resource - az bude funkcni wsdl budeme cist z neho
        SEXml.Load(General.WorkingDirectory & "Settings\SoapEnvelope.xml")
        ' pripravime telo Soap dotazu
        Dim SoapNode As XmlNode = SEXml.SelectSingleNode("/SOAP:Envelope/SOAP:Body", nsmgr)
        ' a zpravu vlozime do Soap obalky
        SoapNode.AppendChild(SEXml.ImportNode(MEXml.DocumentElement, True))
        ' a vratime cely xml dokument - kompletni dotaz na inbox

        Return SEXml
    End Function

    ''' <summary>
    ''' jiz nepodporovano, po odstraneni vsech volani teto funkce bude odstranena
    ''' </summary>
    ''' <param name="Urlx"></param>
    ''' <param name="User"></param>
    ''' <param name="Password"></param>
    ''' <param name="MyXml"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function SendRequest(ByVal Urlx As String, ByVal User As String, ByVal Password As String, ByVal MyXml As XmlDocument) As XmlDocument
        General.Log("Dotazuji (SendRequest): " + Urlx, Me)
        Dim HisXml As XmlDocument = New XmlDocument
        Try
            Dim Request As HttpWebRequest = WebRequest.Create(Urlx)

            Request.Method = "POST"
            Request.Headers.Add("SOAPAction", "http://sap.com/xi/WebService/soap1.1")
            Request.Headers.Add("Accept-Encoding", "deflate")
            ' Rq.MaximumResponseHeadersLength = 512
            Request.ContentType = "text/xml;charset=utf-8"
            Request.UserAgent = "INSYCO Client 2.0.0.1"
            Request.ServicePoint.Expect100Continue = False


            Dim reqBuff As Byte() = System.Text.UTF8Encoding.UTF8.GetBytes(MyXml.InnerXml)

            Request.ContentLength = reqBuff.Length
            Request.Timeout = General.TimeOut
            Request.Credentials = New NetworkCredential(User, Password, "")
            Request.ClientCertificates.Add(General.ClientCertificate)
            'Request.ClientCertificates.Add(New X509Certificate(My.Resources.postsignum_qca2_root))
            'Request.ClientCertificates.Add(New X509Certificate(My.Resources.testportal))

            Dim reqStream As Stream = Request.GetRequestStream()
            reqStream.Write(reqBuff, 0, reqBuff.Length)

            Dim Response As HttpWebResponse = CType(Request.GetResponse(), HttpWebResponse)
            Dim memStream As MemoryStream = New MemoryStream()
            Const BUFFER_SIZE As Integer = 4096
            Dim iRead As Integer = 0
            Dim idx As Integer = 0
            Dim iSize As Int64 = 0

            memStream.SetLength(BUFFER_SIZE)

            While (True)
                Dim resBuffer As Byte() = New Byte(BUFFER_SIZE) {}
                Try
                    iRead = Response.GetResponseStream().Read(resBuffer, 0, BUFFER_SIZE)
                Catch e As System.Exception
                    General.Log(e.Message, Me)
                    Return MakeErrorAnswer(501, "Chyba při příjmu dotazu", e.Message)
                End Try

                If iRead = 0 Then
                    Exit While
                End If
                iSize += iRead
                memStream.SetLength(iSize)
                memStream.Write(resBuffer, 0, iRead)
                idx += iRead
            End While

            Dim content As Byte() = memStream.ToArray()
            memStream.Close()

            Dim strResp As String = System.Text.UTF8Encoding.UTF8.GetString(content)
            Try
                HisXml.LoadXml(strResp)
            Catch e As System.Exception
                Return MakeErrorAnswer(200, "Úspěšně odesláno", "HTML 200 OK")
            End Try

        Catch webE As WebException
            General.Log(webE.Message, Me)
            Return MakeErrorAnswer(500, "Chyba při odesílání", webE.Message)

        End Try

        General.Log("Konec requestu OK", Me)
        Return HisXml
    End Function

    ''' <summary>
    ''' Z historickych duvodu - bude smazana
    ''' </summary>
    ''' <param name="MyXml"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ReadInbox(ByVal MyXml As XmlDocument) As XmlDocument
        General.Log("Dotazuji (ReadInbox): https://portal5.statnipokladna.cz/csuis/wstest/inbox", Me)
        Dim HisXml As XmlDocument = New XmlDocument
        Try
            Dim Request As HttpWebRequest = WebRequest.Create("https://portal5.statnipokladna.cz/csuis/wstest/inbox")
            Request.Method = "POST"
            Request.Headers.Add("SOAPAction", "http://sap.com/xi/WebService/soap1.1")
            Request.ContentType = "text/xml"
            Dim reqBuff As Byte() = System.Text.UTF8Encoding.UTF8.GetBytes(MyXml.InnerXml)
            Request.ContentLength = reqBuff.Length
            Request.Timeout = General.TimeOut
            Request.Credentials = New NetworkCredential("2000000002", "lr3zr6c5", "")
            Dim reqStream As Stream = Request.GetRequestStream()
            reqStream.Write(reqBuff, 0, reqBuff.Length)
            Dim Response As HttpWebResponse = CType(Request.GetResponse(), HttpWebResponse)
            Dim memStream As MemoryStream = New MemoryStream()
            Const BUFFER_SIZE As Integer = 4096
            Dim iRead As Integer = 0
            Dim idx As Integer = 0
            Dim iSize As Int64 = 0
            memStream.SetLength(BUFFER_SIZE)
            While (True)
                Dim resBuffer As Byte() = New Byte(BUFFER_SIZE) {}
                Try
                    iRead = Response.GetResponseStream().Read(resBuffer, 0, BUFFER_SIZE)
                Catch e As System.Exception
                    Return MakeErrorAnswer(501, "Chyba při příjmu dotazu", e.Message)
                End Try

                If iRead = 0 Then
                    Exit While
                End If
                iSize += iRead
                memStream.SetLength(iSize)
                memStream.Write(resBuffer, 0, iRead)
                idx += iRead
            End While

            Dim content As Byte() = memStream.ToArray()
            memStream.Close()

            Dim strResp As String = System.Text.UTF8Encoding.UTF8.GetString(content)
            HisXml.LoadXml(strResp)

        Catch webE As WebException
            Return MakeErrorAnswer(500, "Chyba při komunikaci", webE.Message)
        End Try

        Return HisXml
    End Function

    ''' <summary>
    ''' Stahuje tělo zprávy.
    ''' </summary>
    ''' <param name="id">Identifikační číslo zprávy získané pomocí <see cref="GetMessagesHeaders"></see></param>
    ''' <returns>Řetězcová repreyentace Xml dokumentu podle definic IISSP</returns>
    ''' <remarks>Tuto funkci je možné využít pro stahohování zpráv jak z CSUIS Inbox tak RIS Inbox</remarks>
    Public Function GetMessageById(ByVal id As String) As String
        General.Log("Volám: GetMessageById", Me)
        Dim doc As XmlDocument = New XmlDocument
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)

        nsmgr.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        doc.Load(General.WorkingDirectory & "Settings\MessageByIdRequest.xml")
        doc.SelectSingleNode("//msg:ZpravaId", nsmgr).InnerText = id
        General.SetMyRequestToIisspXml(doc.DocumentElement)
        Return General.Request
    End Function

    ''' <summary>
    ''' Stáhne hlavičky zpráv dle parametrů
    ''' </summary>
    ''' <param name="HromadneZpravy">musí být '1' nebo '0'</param>
    ''' <param name="DatumVytvoreniOd">musí být ve tvaru 'rrrr-mm-dd'</param>
    ''' <param name="DatumVytvoreniDo">musí být ve tvaru 'rrrr-mm-dd'</param>
    ''' <param name="ZpravaStatus">'R'- přečtené 'N' - nepřečtené ''</param>
    ''' <param name="TypDatoveZpravy"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetMessagesHeaders(ByVal HromadneZpravy As String, _
                                ByVal DatumVytvoreniOd As String, _
                                ByVal DatumVytvoreniDo As String, _
                                ByVal ZpravaStatus As String, _
                                ByVal TypDatoveZpravy As String) As String
        General.Log("Volám: GetMessagesHeaders", Me)
        General.SenderResponsiblePersonId = "2000000002"
        Dim doc As XmlDocument = New XmlDocument
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)

        nsmgr.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")

        If General.RecipientModule = "CSUIS" Then
            doc.Load(General.WorkingDirectory & "Settings\CsuisInboxGetMessagesHeadersLayout.xml")
            doc.SelectSingleNode("//msg:ZpravaStatus", nsmgr).InnerText = ZpravaStatus
            doc.SelectSingleNode("//msg:TypDatoveZpravy", nsmgr).InnerText = TypDatoveZpravy
        Else
            doc.Load(General.WorkingDirectory & "Settings\RisreInboxGetMessagesHeadersLayout.xml")
        End If

        doc.SelectSingleNode("//msg:ZobrazitHromadneZpravy", nsmgr).InnerText = HromadneZpravy
        doc.SelectSingleNode("//msg:ZpravaDatumVytvoreniOd", nsmgr).InnerText = DatumVytvoreniOd
        doc.SelectSingleNode("//msg:ZpravaDatumVytvoreniDo", nsmgr).InnerText = DatumVytvoreniDo
        General.MyRequest = MakeInboxRequestXml(doc.DocumentElement).OuterXml
        Return General.Request
    End Function

    ''' <summary>
    ''' z testovních dúvodú, bude odstraněna
    ''' </summary>
    ''' <param name="Url"></param>
    ''' <param name="User"></param>
    ''' <param name="Password"></param>
    ''' <param name="Msg"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetTestRequest(ByVal Url As String, ByVal User As String, ByVal Password As String, ByVal Msg As XmlDocument)

        General.Log("Volám: GetTestRequest", Me)

        Dim doc As XmlDocument = New XmlDocument
        doc = SendRequest(Url, User, Password, Msg)

        Return doc.OuterXml.ToString
    End Function

End Class


