Imports System.Security.Cryptography.X509Certificates
Imports System.Security.Cryptography
Imports System.Security.Cryptography.Xml
Imports System.IO
Imports System.Xml
Imports System.Net
Imports System.Text
Imports Krystalware.UploadHelper

''' <summary> 
''' Základní třída komponenty. Umožňuje obecné volání služeb, buď přímo, pro podrobněji nedefinované služby, nebo 
''' především pomocí jiných tříd, které přesněji specifikují dotaz a třídu General využívají jako komunikační
''' prostředek. Například třída <see cref="IISSPInbox"></see>. Ty třídy disponují vlastnosí General, která je instancí IISSPGeneral.
''' Pomocí vlastností třídy se nastaví parametry služby a pomocí fce <see cref="IISSPGeneral.Request"></see> se vykonná dotaz.
''' Definuje další funkce jako <see cref="IISSPGeneral.FormatXml">formátování Xml</see>, generuje <see cref="IISSPGeneral.GetGuid">GUID</see>, vrací <see cref="IISSPGeneral.GetDateTime">
''' datum a čas</see>, nebo <see cref="IISSPGeneral.GetDate">datum</see> ve správném formátu atd.
''' Zajišťuje cryptografické funkce jako <see cref="IISSPGeneral.SignXml">digitální podepisování</see>.
''' </summary>
<ComClass(IISSPGeneral.ClassId, IISSPGeneral.InterfaceId, IISSPGeneral.EventsId)> _
Public Class IISSPGeneral

    'Public ClientCertificate As X509Certificate2  'Klientsky certifikát využívaný pro připojení, je vytvořen z ClientCertificatePath a PWD

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "cefdc725-91b2-40ce-a28a-c00b83d9ba42"
    Public Const InterfaceId As String = "27d5c7fa-0a13-43f3-ac49-8897c6850664"
    Public Const EventsId As String = "72aa5823-c3d9-4bc4-86b6-1230f65157bc"
#End Region

    ' A creatable COM class must have a Public Sub New() 
    ' with no parameters, otherwise, the class will not be 
    ' registered in the COM registry and cannot be created 
    ' via CreateObject.

    Private _Url As String
    ''' <summary>
    ''' Url služby
    ''' </summary>
    ''' <value>Hodnota</value>
    ''' <returns>Url aktuálně definované služby.</returns>
    ''' <remarks>V případě, že bude volána vlastnot <see cref="Service"/>, není potřeba tuto vlastnost nastavovat.
    ''' Pokud nebude nastavene, načte se její hodnota z nastavení, kde je uložena posleně použitá hodnota.</remarks>
    Public Property Url() As String
        Get
            If _Url Is Nothing Then
                _Url = My.Settings.Url
            End If
            Return _Url
        End Get
        Set(ByVal value As String)
            _Url = value
            My.Settings.Url = value
        End Set
    End Property

    Private _Service As String
    ''' <summary>
    ''' Tuto vlastnost voláme pro nastavení parametrů služby z definice.
    ''' </summary>
    ''' <value>Cesta k WSDL definici služby.</value>
    ''' <remarks>Tato vlastnost neni v současné době implementována</remarks>
    Public WriteOnly Property Service() As String
        Set(ByVal value As String)
            ' budeme nacitat s wsdl az budou jasne
            _Service = value
        End Set
    End Property


    Private _ClientCertificatePath As String
    ''' <summary>
    ''' Tato vlastnost umožňuje nastavit cestu a jméno, kde se nachází klientský certifikát potřebný pro SSL/TLS spojení. např. C:\IISSP\CERT\MujCertif.pfx
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ClientCertificatePath As String
        Set(ByVal value As String)
            _ClientCertificatePath = value
            My.Settings.ClientCertificatePath = value
        End Set
        Get
            If _ClientCertificatePath Is Nothing Then
                _ClientCertificatePath = My.Settings.ClientCertificatePath
            End If
            Return _ClientCertificatePath
        End Get
    End Property

    Private _ClientCertificatePWD As String
    ''' <summary>
    ''' Tato vlastnost umožňuje nastavit heslo ke klientskému certifikatu
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ClientCertificatePWD As String
        Set(ByVal value As String)
            _ClientCertificatePWD = value
            My.Settings.ClientCertificatePWD = value
        End Set
        Get
            If _ClientCertificatePWD Is Nothing Then
                _ClientCertificatePWD = ""
            End If
            Return _ClientCertificatePWD
        End Get
    End Property

    Private _ClientCertificate As X509Certificate2
    Public Property ClientCertificate() As X509Certificate2
        Set(ByVal value As X509Certificate2)
            _ClientCertificate = value
            My.Settings.ClientCertificate = value
        End Set
        Get
            If _ClientCertificate Is Nothing Then
                _ClientCertificate = My.Settings.ClientCertificate
            End If
            Return _ClientCertificate
        End Get
    End Property

    Public ServerCertificate As X509Certificate2 = My.Settings.ServerCertificate
    Public CAClient As X509Certificate2 = My.Settings.CAClient
    Public CAServer As X509Certificate2 = My.Settings.CAServer

    Private _UserName As String
    ''' <summary>
    ''' Zadáváme nebo čteme uživatelské jméno pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property UserName() As String
        Set(ByVal value As String)
            _UserName = value
            My.Settings.UserName = value
        End Set
        Get
            If _UserName Is Nothing Then
                _UserName = My.Settings.UserName
            End If
            Return _UserName
        End Get
    End Property

    Private _Password As String
    ''' <summary>
    ''' Zadáváme nebo čteme heslo pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Password() As String
        Set(ByVal value As String)
            _Password = value
            My.Settings.Password = value
        End Set
        Get
            If _Password Is Nothing Then
                _Password = My.Settings.Password
            End If
            Return _Password
        End Get
    End Property

    Private _SenderResponsiblePersonEmail As String
    ''' <summary>
    ''' Zadáváme nebo čteme SenderResponsiblePersonEmail pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SenderResponsiblePersonEmail() As String
        Set(ByVal value As String)
            _SenderResponsiblePersonEmail = value
            My.Settings.SenderResponsiblePersonEmail = value
        End Set
        Get
            If _SenderResponsiblePersonEmail Is Nothing Then
                _SenderResponsiblePersonEmail = My.Settings.SenderResponsiblePersonEmail
            End If
            Return _SenderResponsiblePersonEmail
        End Get
    End Property

    Private _SenderResponsiblePersonId As String
    ''' <summary>
    ''' Zadáváme nebo čteme SenderResponsiblePersonId pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SenderResponsiblePersonId() As String
        Set(ByVal value As String)
            _SenderResponsiblePersonId = value
            My.Settings.SenderResponsiblePersonId = value
        End Set
        Get
            If _SenderResponsiblePersonId Is Nothing Then
                _SenderResponsiblePersonId = My.Settings.SenderResponsiblePersonId
            End If
            Return _SenderResponsiblePersonId
        End Get
    End Property

    Private _SenderResponsiblePersonName As String
    ''' <summary>
    ''' Zadáváme nebo čteme SenderResponsiblePersonName pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SenderResponsiblePersonName() As String
        Set(ByVal value As String)
            _SenderResponsiblePersonName = value
            My.Settings.SenderResponsiblePersonName = value
        End Set
        Get
            If _SenderResponsiblePersonName Is Nothing Then
                _SenderResponsiblePersonName = My.Settings.SenderResponsiblePersonName
            End If
            Return _SenderResponsiblePersonName
        End Get
    End Property

    Private _SenderIc As String
    ''' <summary>
    ''' Zadáváme nebo čteme SenderIc pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SenderIc() As String
        Set(ByVal value As String)
            _SenderIc = value
            My.Settings.SenderIc = value
        End Set
        Get
            If _SenderIc Is Nothing Then
                _SenderIc = My.Settings.SenderIc
            End If
            Return _SenderIc
        End Get
    End Property

    Private _SenderSubjectName As String
    ''' <summary>
    ''' Zadáváme nebo čteme SenderSubjectName pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SenderSubjectName() As String
        Set(ByVal value As String)
            _SenderSubjectName = value
            My.Settings.SenderSubjectName = value
        End Set
        Get
            If _SenderSubjectName Is Nothing Then
                _SenderSubjectName = My.Settings.SenderSubjectName
            End If
            Return _SenderSubjectName
        End Get
    End Property

    Private _RecipientIc As String
    ''' <summary>
    ''' Zadáváme nebo čteme RecipientIc pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property RecipientIc() As String
        Set(ByVal value As String)
            _RecipientIc = value
            My.Settings.RecipientIc = value
        End Set
        Get
            If _RecipientIc Is Nothing Then
                _RecipientIc = My.Settings.RecipientIc
            End If
            Return _RecipientIc
        End Get
    End Property

    Private _RecipientModule As String
    ''' <summary>
    ''' Zadáváme nebo čteme RecipientModule pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property RecipientModule() As String
        Set(ByVal value As String)
            _RecipientModule = value
            My.Settings.RecipientModule = value
        End Set
        Get
            If _RecipientModule Is Nothing Then
                _RecipientModule = My.Settings.RecipientModule
            End If
            Return _RecipientModule
        End Get
    End Property

    Private _RecipientSubjectName As String
    ''' <summary>
    ''' Zadáváme nebo čteme RecipientSubjectName pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property RecipientSubjectName() As String
        Set(ByVal value As String)
            _RecipientSubjectName = value
            My.Settings.RecipientSubjectName = value
        End Set
        Get
            If _RecipientSubjectName Is Nothing Then
                _RecipientSubjectName = My.Settings.RecipientSubjectName
            End If
            Return _RecipientSubjectName
        End Get
    End Property
    'Public Shared Url_FMMD As String = My.Settings.Url_FMMD
    'Public Shared Url_EKIS_SP_ROP As String = My.Settings.Url_EKIS_SP_ROP
    'Public Shared Url_SP_EKIS_ROP As String = My.Settings.Url_SP_EKIS_ROP

    Private _Url_FMMD As String
    ''' <summary>
    ''' Zadáváme nebo čteme Url pro FMMD např. https://testportal3.statnipokladna.cz/risre/ws/B_SP_EKIS_FMMD pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Url_FMMD() As String
        Set(ByVal value As String)
            _Url_FMMD = value
            My.Settings.Url_FMMD = value
        End Set
        Get
            If _Url_FMMD Is Nothing Then
                _Url_FMMD = My.Settings.Url_FMMD
            End If
            Return _Url_FMMD
        End Get
    End Property

    Private _Url_EKIS_SP_ROP As String
    ''' <summary>
    ''' Zadáváme nebo čteme Url pro FMMD např. https://testportal3.statnipokladna.cz/risre/ws/B_EKIS_SP_ROP pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Url_EKIS_SP_ROP() As String
        Set(ByVal value As String)
            _Url_EKIS_SP_ROP = value
            My.Settings.Url_EKIS_SP_ROP = value
        End Set
        Get
            If _Url_EKIS_SP_ROP Is Nothing Then
                _Url_EKIS_SP_ROP = My.Settings.Url_EKIS_SP_ROP
            End If
            Return _Url_EKIS_SP_ROP
        End Get
    End Property

    Private _Url_SP_EKIS_ROP As String
    ''' <summary>
    ''' Zadáváme nebo čteme Url pro FMMD např. https://testportal3.statnipokladna.cz/risre/ws/B_SP_EKIS_ROP pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Url_SP_EKIS_ROP() As String
        Set(ByVal value As String)
            _Url_SP_EKIS_ROP = value
            My.Settings.Url_SP_EKIS_ROP = value
        End Set
        Get
            If _Url_SP_EKIS_ROP Is Nothing Then
                _Url_SP_EKIS_ROP = My.Settings.Url_SP_EKIS_ROP
            End If
            Return _Url_SP_EKIS_ROP
        End Get
    End Property

    Private _Url_EKIS_SP_ROP_ATT As String
    ''' <summary>
    ''' Zadáváme nebo čteme Url pro FMMD např. https://testportal3.statnipokladna.cz/risre/ws/B_EKIS_ROP_ATT pokud je vyžadováno volanou službou.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Url_EKIS_SP_ROP_ATT() As String
        Set(ByVal value As String)
            _Url_SP_EKIS_ROP = value
            My.Settings.Url_EKIS_SP_ROP_ATT = value
        End Set
        Get
            If _Url_EKIS_SP_ROP_ATT Is Nothing Then
                _Url_EKIS_SP_ROP_ATT = My.Settings.Url_EKIS_SP_ROP_ATT
            End If
            Return _Url_EKIS_SP_ROP_ATT
        End Get
    End Property

    Private _WorkingDirectory As String
    ''' <summary>
    ''' Název kořenové pracovní složky knihovny.
    ''' </summary>
    ''' <value>Řetězec jako název složky</value>
    ''' <returns>Vrací název kořenové pracovní složky knihovny jako <c>String</c></returns>
    ''' <remarks>V této složce se vytvářejí posložky. Např \log pro logovací soubory, \Settings pro nastavení a pod.</remarks>
    Public Property WorkingDirectory() As String
        Set(ByVal value As String)
            _WorkingDirectory = value
            My.Settings.WorkingDirectory = value
        End Set
        Get
            If _WorkingDirectory Is Nothing Then
                _WorkingDirectory = My.Settings.WorkingDirectory
            End If
            Return _WorkingDirectory
        End Get
    End Property

    Private _Loging As Boolean
    ''' <summary>
    ''' Tato vlastnost určuje zda je aktivní logování či nikoli.
    ''' </summary>
    ''' <value>Nastavením vlastnosti na <c>True</c> zapneme logování. <c>False</c>, logování vypnuto</value>
    ''' <returns><c>True</c>, jestli je logování zapnuto. <c>False</c>, jeli logování vypnuto</returns>
    ''' <remarks></remarks>
    Public Property Loging() As Boolean
        Set(ByVal value As Boolean)
            _Loging = value
        End Set
        Get
            If Not _Loging Then
                _Loging = My.Settings.Loging
            End If
            Return _Loging
        End Get
    End Property

    Private _MyRequest As String
    ''' <summary>
    ''' Zadáváme nebo čteme MyRequest pokud je vyžadováno volanou službou.
    ''' Řetězcová reprezentace Xml dotazu zasílaná jako tělo dotazu po volání <see cref="Request"></see>
    ''' Po vykonání dotazu je do ní uložen výsledek.
    ''' </summary>
    ''' <value>Řetězcová reprezentace Xml dotazu</value>
    ''' <returns>Řetězcová reprezentace Xml odpovědi</returns>
    ''' <remarks></remarks>
    Public Property MyRequest() As String
        Set(ByVal value As String)
            _MyRequest = value
        End Set
        Get

            Return _MyRequest
        End Get
    End Property

    Private _TimeOut As Integer
    ''' <summary>
    ''' Nastavujeme časovou prodlevu dotazu v milisekundách. Pokud nebude ve stanoveném čase doručena odpověď, 
    ''' bude vygenerována interní chybová zpráva nesoucí popis systémové vyjímky.
    ''' </summary>
    ''' <value>Délka v milisekundách</value>
    ''' <returns><para>Integer</para>, jako délka v milisekundách</returns>
    ''' <remarks></remarks>
    Public Property TimeOut() As Integer
        Set(ByVal value As Integer)
            _TimeOut = value
            My.Settings.TimeOut = value
        End Set
        Get
            If _TimeOut = 0 Then
                _TimeOut = My.Settings.TimeOut
            End If
            Return _TimeOut
        End Get
    End Property

    Private _NamespaceManager As XmlNamespaceManager
    ''' <summary>
    ''' Defaulní nastavení názvových prostorů používaných při komunikaci s IISSP.
    ''' </summary>
    ''' <returns>Všechny názvové prostory, v objektu NamespaceManager</returns>
    ''' <remarks></remarks>
    Public Property NamespaceManager() As XmlNamespaceManager
        Set(ByVal value As XmlNamespaceManager)
            _NamespaceManager = value
        End Set
        Get
            Return _NamespaceManager
        End Get
    End Property

    Private _RefGuid As String
    ''' <summary>
    ''' Pouze ke čtení. Po vygenerování komunikační obálky, kdy je generován jedinečný identifikátor <see cref="Guid"></see> se 
    ''' zde GUID uloží. Slouží pro pozdější spárování s odpověďmi. 
    ''' </summary>
    ''' <returns>GUID posledně vygenerované během sestavení dotazu.</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property RefGuid() As String
        Get
            Return _RefGuid
        End Get
    End Property

    Public Sub New()
        MyBase.New()
        Try
            MkDir(WorkingDirectory + "log")
            Log("Vytvořena složka " + WorkingDirectory + "log.", Me)
        Catch
            Log("Ověřena složka " + WorkingDirectory + "log.", Me)
        End Try
        Log("Inicializace Třídy IISSPGeneral", Me)
        ' zatim nacitame z resource dokud nebude jasne odkud se budou nacitat
        'ClientCertificate = New X509Certificate2(My.Resources.tiba, "tiba")
        _NamespaceManager = New XmlNamespaceManager(New NameTable)
        _NamespaceManager.AddNamespace("SOAP", "http://schemas.xmlsoap.org/soap/envelope/")
        _NamespaceManager.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        _NamespaceManager.AddNamespace("cmn", "urn:cz:mfcr:iissp:schemas:Common:v1")

    End Sub

    ''' <summary>
    ''' Zápis do logu knihovny. 
    ''' </summary>
    ''' <param name="MsgTxt">Text <paramref name="MsgTxt"/>, který se zapíše do logu.</param>
    ''' <param name="O">Objekt <paramref name="O"/> jehož HashCode bude přiřazen k příslušnému záznamu v logu.</param>
    ''' <returns><c>True</c> jestli jestli je záznam úspěšně zalogován,
    ''' <c>False</c> jestli se zápis nezdařil.</returns>
    ''' <remarks>Zápis do logu je povolen nastavením vlastnosti <see cref="Loging"/> na <c>True</c>. 
    ''' A cesta k souboru "Log.txt" se nastavuje ve vlastnosti <see cref="WorkingDirectory"/> ve které se vytvoří složka "log".
    ''' <seealso cref="Loging"/><seealso cref="WorkingDirectory"/></remarks>
    Public Function Log(ByVal MsgTxt As String, ByVal O As Object) As Boolean
        If Not Loging Then
            Return False
        End If

        Try
            Dim outFile As TextWriter = File.AppendText(WorkingDirectory + "log\log.txt")
            outFile.WriteLine(" *** " + Now.ToString("yyyy.MM.dd hh:mm:ss.fff: ") + MsgTxt + " (Object: " + O.ToString + ") " + O.GetHashCode.ToString)
            outFile.Close()
            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Naformátuje Xml.
    ''' </summary>
    ''' <param name="InXml">Vstupní Xml dokument</param>
    ''' <returns>Naformátované Xml</returns>
    Public Function FormatXml(ByVal InXml As XmlDocument) As XmlDocument
        Dim ms As MemoryStream = New MemoryStream()
        Dim XW As XmlTextWriter = New XmlTextWriter(ms, Encoding.UTF8)
        XW.WriteStartDocument()
        XW.Formatting = Formatting.Indented
        InXml.WriteContentTo(XW)
        XW.WriteEndDocument()
        XW.Flush()
        ms.Seek(0, SeekOrigin.Begin)
        Dim sr As StreamReader = New StreamReader(ms)
        InXml.LoadXml(sr.ReadToEnd)
        Return InXml
    End Function

    ''' <summary>
    ''' Vygeneruje GUID
    ''' </summary>
    ''' <returns>32 znaků dlouhé unikátní číslo</returns>
    Public Function GetGuid() As String
        Dim myGUID As Guid
        Dim myStrGUID As String
        myGUID = Guid.NewGuid
        myStrGUID = myGUID.ToString
        GetGuid = Replace(myStrGUID, "-", "")
    End Function

    ''' <summary>
    ''' Vrátí datum a čas ve formátu, který vyžaduje IISSP
    ''' </summary>
    ''' <param name="DatumCas">Pokud je parametr prázdný, bude vrácen aktuální datum a čas.</param>
    ''' <returns>Aktuální datum ve formátu "yyyy-mm-ddThh:mm:ssZ"</returns>
    ''' <remarks></remarks>
    Public Function GetDateTime(Optional ByVal DatumCas As Date = #1/1/1000 12:01:00 AM#) As String
        If DatumCas = #1/1/1000 12:01:00 AM# Then
            DatumCas = Now
        End If
        Return DatumCas.ToString("s") + "Z"
    End Function

    ''' <summary>
    ''' Vrátí datum ve formátu, který vyžaduje IISSP
    ''' </summary>
    ''' <param name="Datum">Pokud je parametr prázdný, bude vrácen aktuální datum.</param>
    ''' <returns>Aktuální datum ve formátu "yyyy-mm-dd"</returns>
    Public Function GetDate(Optional ByVal Datum As Date = #1/1/1000#) As String
        If Datum = #1/1/1000# Then
            Datum = Now
        End If
        Return Datum.ToString("yyyy-MM-dd")
    End Function

    ''' <summary>
    ''' Vytvoří xml hlášení podle parametrů
    ''' </summary>
    ''' <param name="number">Interní číslo zprávy</param>
    ''' <param name="name">Interní název zprávy</param>
    ''' <param name="popis">Externí hlášení</param>
    ''' <returns>Vrátí hlášení</returns>
    ''' <remarks></remarks>
    Private Function MakeErrorAnswer(ByVal number As Integer, ByVal name As String, ByVal popis As String) As XmlDocument
        Dim ErrXml As XmlDocument = New XmlDocument

        'ErrXml.LoadXml(My.Resources.ErrorRequest)
        ErrXml.Load(WorkingDirectory & "Settings\ErrorRequest.xml")
        ErrXml.SelectSingleNode("//Popis").InnerText = popis
        ErrXml.SelectSingleNode("//Nazev").InnerText = name
        ErrXml.SelectSingleNode("//Number").InnerText = number.ToString
        Log(ErrXml.InnerXml, Me)
        Return ErrXml
    End Function

    ''' <summary>
    ''' Vytvoří digitální podpis xml dokumentu, elementu "EnvelopeBody"
    ''' Podpis se realizuje soukromým klíčem z certifikátu <see cref="ClientCertificate"></see>
    ''' </summary>
    ''' <param name="InXml">XmlDocument, který má být podepsaný</param>
    ''' <returns>Vrátí podepsaný XmlDokument</returns>
    ''' <remarks>Zatím nebyl zveřejněn přesný formát podpisu. Transformace a formát je nastaven jako v případě Identifikátoru celistvosti.</remarks>
    Public Function SignXml(ByVal InXml As XmlDocument) As XmlDocument
        ' vytvorime root element z naseho xml
        Dim root As XmlElement = InXml.DocumentElement
        ' vytvorime si node
        Dim EnvelopeFooter As XmlElement = InXml.CreateElement("msg", "EnvelopeFooter", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        ' a celej footer element vlozime do dokumentu
        EnvelopeFooter.InnerText = ""
        root.AppendChild(EnvelopeFooter)
        ' vytvorime podpisovy element pro nas dokument
        Dim SignedXml As New SignedXml(InXml)
        SignedXml.SigningKey = ClientCertificate.PrivateKey
        ' nastavime predepsane id podpisu
        SignedXml.Signature.Id = "ElektronickyPodpis"
        ' Specifikujeme kanonikalizacni metodu
        SignedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
        ' Vytvarime reference na cely xml dokument
        Dim reference As New Reference("")
        ' vyjmeme signature
        reference.AddTransform(New XmlDsigEnvelopedSignatureTransform())
        ' aplikujeme kanonikalizaci
        reference.AddTransform(New XmlDsigExcC14NWithCommentsTransform())
        ' pridavame reference do elementu s podpisem.
        SignedXml.AddReference(reference)
        ' vytvorime podpis.
        SignedXml.ComputeSignature()
        ' vlozime do nej podpis
        EnvelopeFooter.AppendChild(InXml.ImportNode(SignedXml.GetXml(), True))
        Return InXml
    End Function


    ''' <summary>
    ''' Vlastní volání dotazu. Předpokládá správné nastavení všech vlastností ovlivňující přenos.
    ''' Tělo dotazu načítá z vlastnosti <see cref="MyRequest"></see>
    ''' </summary>
    ''' <returns>Řetězcovou reprezentaci Xml dokumentu</returns>
    ''' <remarks>Výsledek také ukláda do <see cref="MyRequest"></see></remarks>
    Public Function Requestback() As String
        Log("Dotazuji (Request): ", Me)
        Dim HisXml As XmlDocument = New XmlDocument
        HisXml.LoadXml(MyRequest)
        Try
            Dim Rq As HttpWebRequest = WebRequest.Create(Url)
            Rq.Method = "POST"
            Rq.Headers.Add("SOAPAction", "http://sap.com/xi/WebService/soap1.1")
            Rq.Headers.Add("Accept-Encoding", "gzip,deflate")
            Rq.KeepAlive = True
            'Rq.MaximumResponseHeadersLength = 512
            'Rq.ContentType = "text/xml;charset=utf-8"
            Rq.UserAgent = "INSYCO Client 2.0.0.1"
            Rq.ServicePoint.Expect100Continue = False
            'Rq.PreAuthenticate = True
            'Rq.AuthenticationLevel = Security.AuthenticationLevel.MutualAuthRequested
            Dim reqBuff As Byte() = System.Text.UTF8Encoding.UTF8.GetBytes(MyRequest)
            'Rq.ContentLength = reqBuff.Length
            Rq.Timeout = TimeOut
            Rq.Credentials = New NetworkCredential(UserName, Password, "")
            Rq.ClientCertificates.Add(New X509Certificate2("c:\iissp\settings\tiba.pfx", "tiba"))
            'Dim reqStream As Stream = Rq.GetRequestStream()
            'reqStream.Write(reqBuff, 0, reqBuff.Length)

            Dim files() As UploadFile = {New UploadFile("c:\iissp\settings\body.xml", "hokuspokus", "text/xml;charset=utf-8"),
                                         New UploadFile("c:\iissp\settings\attachment-by-tyba.xml", Nothing, "text/xml;charset=utf-8")}
            Dim colec As Specialized.NameValueCollection = New Specialized.NameValueCollection()
            'colec.Add("pok", "pook")

            Log("Request odeslán:", Me)
            Dim rqx As HttpWebRequest = WebRequest.Create(Url)
            Dim Response As HttpWebResponse = HttpUploadHelper.Upload(Rq, files, colec)
            'Dim Response As HttpWebResponse = CType(Rq.GetResponse(), HttpWebResponse)
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
                    Response.Close()
                    memStream.Close()
                    HisXml = MakeErrorAnswer(501, "Chyba při příjmu dotazu", e.Message)
                    Return HisXml.OuterXml
                End Try

                If iRead = 0 Then
                    Exit While
                End If
                iSize += iRead
                memStream.SetLength(iSize)
                memStream.Write(resBuffer, 0, iRead)
                idx += iRead
                Log("Přijato bytu: " + idx.ToString, Me)
            End While


            Dim content As Byte() = memStream.ToArray()
            Response.Close()
            memStream.Close()

            Dim strResp As String = System.Text.UTF8Encoding.UTF8.GetString(content)
            Try
                HisXml.LoadXml(strResp)
            Catch e As System.Exception
                HisXml = MakeErrorAnswer(200, "Úspěšně odesláno", "HTML 200 OK")

                Return HisXml.OuterXml
            End Try
        Catch webE As WebException
            HisXml = MakeErrorAnswer(500, "Chyba při odesílání", webE.Message)
            Return HisXml.OuterXml
        End Try

        Log("Konec requestu OK", Me)
        MyRequest = HisXml.OuterXml
        Return HisXml.OuterXml
    End Function

    ''' <summary>
    ''' Vlastní volání dotazu. Předpokládá správné nastavení všech vlastností ovlivňující přenos.
    ''' Tělo dotazu načítá z vlastnosti <see cref="MyRequest"></see>
    ''' </summary>
    ''' <returns>Řetězcovou reprezentaci Xml dokumentu</returns>
    ''' <remarks>Výsledek také ukláda do <see cref="MyRequest"></see></remarks>
    Public Function Request() As String
        Log("Dotazuji (Request): ", Me)
        Dim HisXml As XmlDocument = New XmlDocument
        HisXml.LoadXml(MyRequest)
        Try
            Dim Rq As HttpWebRequest = WebRequest.Create(Url)
            Rq.Method = "POST"
            Rq.Headers.Add("SOAPAction", "http://sap.com/xi/WebService/soap1.1")
            Rq.Headers.Add("Accept-Encoding", "deflate")
            'Rq.MaximumResponseHeadersLength = 512
            Rq.ContentType = "text/xml;charset=utf-8"
            Rq.UserAgent = "INSYCO Client 2.0.0.1"
            Rq.ServicePoint.Expect100Continue = False
            'Rq.PreAuthenticate = True
            'Rq.AuthenticationLevel = Security.AuthenticationLevel.MutualAuthRequested
            Dim reqBuff As Byte() = System.Text.UTF8Encoding.UTF8.GetBytes(MyRequest)
            Rq.ContentLength = reqBuff.Length
            Rq.Timeout = TimeOut
            Rq.Credentials = New NetworkCredential(UserName, Password, "")

            '' Rq.ClientCertificates.Add(New X509Certificate2(My.Resources.tiba, "tiba"))
            Dim reqStream As Stream = Rq.GetRequestStream()
            reqStream.Write(reqBuff, 0, reqBuff.Length)
            Log("Request odeslán:", Me)
            ' Dim ts As StreamWriter = reqStream.ToString

            'Test cteni co posilam

            Dim PostResponse As String = (reqBuff.ToString)

            ' Create a file and write the byte data to a file.
            ' Dim oFileStream As System.IO.FileStream
            ' oFileStream = New System.IO.FileStream("c:\iissp\test.txt", System.IO.FileMode.Create)
            ' oFileStream.Write(reqBuff, 0, reqBuff.Length)
            ' oFileStream.Close()

            ' Read the content.
            'Dim PostResponse As String = oReader.ReadToEnd()

            ' Clean up the streams.





            Dim Response As HttpWebResponse = CType(Rq.GetResponse(), HttpWebResponse)
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
                    Response.Close()
                    memStream.Close()
                    HisXml = MakeErrorAnswer(501, "Chyba při příjmu dotazu", e.Message)
                    Return HisXml.OuterXml
                End Try

                If iRead = 0 Then
                    Exit While
                End If
                iSize += iRead
                memStream.SetLength(iSize)
                memStream.Write(resBuffer, 0, iRead)
                idx += iRead
                Log("Přijato bytu: " + idx.ToString, Me)
            End While


            Dim content As Byte() = memStream.ToArray()
            Response.Close()
            memStream.Close()

            Dim strResp As String = System.Text.UTF8Encoding.UTF8.GetString(content)
            Try
                HisXml.LoadXml(strResp)
            Catch e As System.Exception
                HisXml = MakeErrorAnswer(200, "Úspěšně odesláno", "HTML 200 OK")
                Return HisXml.OuterXml
            End Try
        Catch webE As WebException
            Dim str As MemoryStream = webE.Response.GetResponseStream()
            Dim con As Byte() = str.ToArray()
            Dim strR As String = System.Text.UTF8Encoding.UTF8.GetString(con)
            HisXml = MakeErrorAnswer(500, "Chyba při odesílání", webE.Message + " :: " + strR)
            If strR > "" Then
                Dim xx As XmlDocument = New XmlDocument
                'xx.LoadXml(strR)
                'xx = FormatXml(xx)
                Log(strR, Me)
                Log("ERROR::IISSPGeneral: Protistraně se nepodařilo zprávu zpracovat", Me)
            Else
                Log("ERROR::IISSPGeneral: Nepodařilo se kontaktovat protistranu.", Me)
            End If
            Return HisXml.OuterXml
        End Try

        Log("Konec requestu OK", Me)
        MyRequest = HisXml.OuterXml
        Return HisXml.OuterXml
    End Function

    Protected Overrides Sub Finalize()
        My.Settings.ServerCertificate = ServerCertificate
        My.Settings.CAClient = CAClient
        My.Settings.CAServer = CAServer
        My.Settings.Url_FMMD = Url_FMMD
        My.Settings.Url_EKIS_SP_ROP = Url_EKIS_SP_ROP
        My.Settings.Url_SP_EKIS_ROP = Url_SP_EKIS_ROP
        My.Settings.ClientCertificatePWD = ""
        My.Settings.Save()
        Log("Ukládám nastavení", Me)
        Log("Deaktivuji třídu IISSPGeneral", Me)
        MyBase.Finalize()
    End Sub

    ''' <summary>
    ''' Vygeneruje Soap obálku a do těla vloží parametr.
    ''' </summary>
    ''' <param name="InElement"> jako tělo dotazu</param>
    ''' <returns>XmlDocument</returns>
    ''' <remarks>Samostatně se tato funce uplatní především ve fázi II. Odesílání dokumentů do CSUIS.</remarks>
    Public Function MakeSoapEnvelopeXml(ByVal InElement As XmlElement) As XmlDocument
        ' nacteme soap obalku
        Dim SEXml As XmlDocument = New XmlDocument
        ' nacitame z resource - az bude funkcni wsdl budeme cist z neho
        'SEXml.LoadXml(My.Resources.SoapEnvelope)
        SEXml.Load(WorkingDirectory & "\Settings\SoapEnvelope.xml")
        ' pripravime telo Soap dotazu
        Dim SoapNode As XmlNode = SEXml.SelectSingleNode("/SOAP:Envelope/SOAP:Body", NamespaceManager)
        ' a zpravu vlozime do Soap obalky
        SoapNode.AppendChild(SEXml.ImportNode(InElement, True))
        ' a vratime cely xml dokument - kompletni dotaz na inbox
        Return SEXml
    End Function

    ''' <summary>
    ''' Vytvoří iissp obálku a do těla vloží předaný element. Nastaví hlavičku podle předem definovaných vlastností.
    ''' Generuje <see cref="GetGuid">GUID</see> jako jedinečný identifikátor zprávy, kterým naplní vlastnost
    ''' <see cref="RefGuid"></see> pro případnou referenci na odpověď. 
    ''' </summary>
    ''' <param name="InElement">XmlElement, který bude vložen do komunikační obálky iissp.</param>
    ''' <returns>Vrací XmlDokument doplněný o dostupné vlastnosti.</returns>
    ''' <remarks>Doplní také údaje o odesílateli a příjemci, předem definovaných pomocí vlastností třídy</remarks>
    Public Function MakeIisspEnvelopeXml(ByVal InElement As XmlElement) As XmlDocument
        Log("Generuji SOAP IISSP envelope " + RecipientModule + " INBOX", Me)
        ' nacteme obalku dotazu pro Inbox
        Dim MEXml As XmlDocument = New XmlDocument
        'MEXml.LoadXml(My.Resources.RisreCsuisEnvelopeLayout)
        MEXml.Load(WorkingDirectory & "Settings\RisreCsuisEnvelopeLayout.xml")

        'generujeme GUID a naplnime RefGuid pro referenci
        _RefGuid = GetGuid()
        'a zapiseme ho do hlavicky
        MEXml.SelectSingleNode("//cmn:TransactionId", NamespaceManager).InnerText = _RefGuid
        'nastavime datum
        MEXml.SelectSingleNode("//msg:DateTimeCreated", NamespaceManager).InnerText = GetDateTime()
        'nastavime dalsi udaje
        MEXml.SelectSingleNode("//msg:Sender/cmn:IC", NamespaceManager).InnerText = SenderIc
        MEXml.SelectSingleNode("//msg:Sender/cmn:SubjectName", NamespaceManager).InnerText = SenderSubjectName
        MEXml.SelectSingleNode("//msg:Sender/cmn:ResponsiblePerson/cmn:PersonName", NamespaceManager).InnerText = SenderResponsiblePersonName
        MEXml.SelectSingleNode("//msg:Sender/cmn:ResponsiblePerson/cmn:PersonId", NamespaceManager).InnerText = SenderResponsiblePersonId
        MEXml.SelectSingleNode("//msg:Sender/cmn:ResponsiblePerson/cmn:Email", NamespaceManager).InnerText = SenderResponsiblePersonEmail
        MEXml.SelectSingleNode("//msg:Recipient/cmn:IC", NamespaceManager).InnerText = RecipientIc
        MEXml.SelectSingleNode("//msg:Recipient/cmn:SubjectName", NamespaceManager).InnerText = RecipientSubjectName
        MEXml.SelectSingleNode("//msg:Recipient/cmn:Module", NamespaceManager).InnerText = RecipientModule

        'vybereme body
        Dim MeXmlNode As XmlNode = MEXml.SelectSingleNode("//msg:EnvelopeBody", NamespaceManager)
        ' vlozime vlastni dotaz zaslanej parametrem InboxRequstXml
        MeXmlNode.AppendChild(MEXml.ImportNode(InElement, True))
        ' a vratime
        Return MEXml
    End Function

    ''' <summary>
    ''' Vytvoří kompletní dotaz a uloží ho do vlastnosti <see cref="MyRequest"></see>. Příkazem <see cref="Request"></see> 
    ''' se dotaz vykoná.
    ''' </summary>
    ''' <param name="InElement">Vlasní tělo dotazu předáváme jako parametr.</param>
    ''' <remarks>V případě nutnosti vykonání dotazu s parametrem jako String, je možné volat funkci <see cref="SetMyRequestToIisspStrig"></see>.</remarks>
    Public Sub SetMyRequestToIisspXml(ByVal InElement As XmlElement)
        Dim X As XmlDocument = MakeIisspEnvelopeXml(InElement)
        X = MakeSoapEnvelopeXml(X.DocumentElement)
        MyRequest = X.OuterXml
    End Sub

    ''' <summary>
    ''' Vytvoří kompletní dotaz a uloží ho do vlastnosti <see cref="MyRequest"></see>. Příkazem <see cref="Request"></see> 
    ''' se dotaz vykoná.
    ''' </summary>
    ''' <param name="InElement">Vlasní tělo dotazu předáváme jako parametr.</param>
    ''' <remarks>V případě nutnosti vykonání dotazu s parametrem jako XmlElement, je možné volat funkci <see cref="SetMyRequestToIisspXml"></see>.</remarks>
    Public Sub SetMyRequestToIisspStrig(ByVal InElement As String)
        Dim E As XmlDocument = New XmlDocument
        E.LoadXml(InElement)
        Dim X As XmlDocument = MakeIisspEnvelopeXml(E.DocumentElement)
        X = MakeSoapEnvelopeXml(X.DocumentElement)
        MyRequest = X.OuterXml
    End Sub

    ''' <summary>
    ''' Vytvoří objekt klientský certifikát z cesty uložení a hesla 
    ''' Slouží pro interní zpracování v dll
    ''' </summary>
    Public Function SetClientCertificate(ByVal strCertPath As String, ByVal strPWD As String) As X509Certificate2

        Try
            SetClientCertificate = New X509Certificate2(strCertPath, strPWD)
        Catch ex As Exception
            Log(ex.Message, Me)
        End Try

        ClientCertificate = SetClientCertificate


        Return SetClientCertificate
    End Function

    Public Function SetServerCertificate(ByVal strCertPath As String, ByVal strPWD As String) As X509Certificate2

        Try
            SetServerCertificate = New X509Certificate2(strCertPath, strPWD)
        Catch ex As Exception
            Log(ex.Message, Me)
        End Try

        ServerCertificate = SetServerCertificate

        Return SetServerCertificate
    End Function

    Public Function SetCACertificate(ByVal strCertPath As String, ByVal strPWD As String) As X509Certificate2

        Try
            SetCACertificate = New X509Certificate2(strCertPath, strPWD)
        Catch ex As Exception
            Log(ex.Message, Me)
        End Try

        CAServer = SetCACertificate

        Return SetCACertificate
    End Function

End Class


