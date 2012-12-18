Imports System
Imports System.IO
Imports System.Security.Cryptography
Imports System.Security.Cryptography.Xml
Imports System.Xml
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' Tato třída poskytuje kryptografické funkce.
''' Vytváří kontrolní podpis - identifikátor celistvosti, šifruje a dešifruje xml dokument.
''' </summary>
''' <remarks></remarks>
<ComClass(IISSPCrypto.ClassId, IISSPCrypto.InterfaceId, IISSPCrypto.EventsId)> _
Public Class IISSPCrypto

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "4b577b5e-a8f0-4416-a394-2defd663d5f0"
    Public Const InterfaceId As String = "a9b31c96-d2e4-4d7b-bac3-fa20c16eb43c"
    Public Const EventsId As String = "910e0e0f-882b-4166-93c2-52942973f737"
#End Region

    ' A creatable COM class must have a Public Sub New() 
    ' with no parameters, otherwise, the class will not be 
    ' registered in the COM registry and cannot be created 
    ' via CreateObject.
    Public Sub New()
        MyBase.New()
    End Sub

    Public Function EncryptNoIntegrity(ByVal XmlString As String, ByVal FullPath As String) As String
        Return Encrypt(XmlString, FullPath, False)
    End Function

    ''' <summary>
    ''' Zajistí Identifikátor celistvosti a zakódování xml dokumentu.
    ''' </summary>
    ''' <param name="XmlString">Xml dokument ve formátu string</param>
    ''' <param name="FullPath">Úplná cesta k souboru typu AESKEY.DEC Soubor se může jmenovat libovolně.</param>
    ''' <returns>Řetězec v Base64</returns>
    ''' <remarks></remarks>
    ''' 
    Public Function Encrypt(ByVal XmlString As String, ByVal FullPath As String, Optional ByVal Integrity As Boolean = True) As String
        'Public Function Encrypt(ByVal XmlString As String, ByVal keyBytes() As Byte) As String

        Dim keyBytes() As Byte
        keyBytes = GetAESKEY(FullPath)

        ' sign
        If (Integrity) Then
            XmlString = SignXmlString(XmlString)
        End If
        ' serializace
        Dim Source As Byte()
        Source = Encoding.UTF8.GetBytes(XmlString)
        ' spocteni dodatku
        Dim zbytek As Integer = (16 + Source.Length + 2) Mod 16
        If zbytek = 0 Then
            zbytek = 16
        End If
        'cilove pole
        Dim Destination(16 + Source.Length + 2 + 16 - zbytek - 1) As Byte
        'pridame 16 znaku
        For i = 0 To 15
            Destination(i) = 65
        Next
        'pridame zdrojovy text
        For i = 16 To 16 + Source.Length - 1
            Destination(i) = Source(i - 16)
        Next
        'vlozime konec radky aby sme nasli konec
        Destination(16 + Source.Length - 1 + 1) = 13
        Destination(16 + Source.Length - 1 + 2) = 10
        'a dorovname na nasobek 16
        For i = 16 + Source.Length - 1 + 3 To Destination.Length - 1
            Destination(i) = 65
        Next
        ' encrypting
        Dim initVectorBytes() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        ' vytvorime sifrovaci objekt
        Dim symmetricKey As RijndaelManaged
        symmetricKey = New RijndaelManaged()
        'nastavime typ
        symmetricKey.Mode = CipherMode.CBC
        'odebereme padding, dulezite
        symmetricKey.Padding = System.Security.Cryptography.PaddingMode.None
        'vytvorime enkryptor
        Dim encryptor As ICryptoTransform
        encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes)
        Dim memoryStream As MemoryStream
        memoryStream = New MemoryStream()
        ' vytvorime crypto stram.
        Dim cryptoStream As CryptoStream
        cryptoStream = New CryptoStream(memoryStream, _
                                        encryptor, _
                                        CryptoStreamMode.Write)
        ' Startujeme encrypt.
        cryptoStream.Write(Destination, 0, Destination.Length)
        cryptoStream.FlushFinalBlock()
        ' vytvorime vystupni pole.
        Dim cipherTextBytes As Byte()
        cipherTextBytes = memoryStream.ToArray()
        memoryStream.Close()
        cryptoStream.Close()
        ' konvertujeme encryptovana data do base64
        Dim cipherText As String
        cipherText = Convert.ToBase64String(cipherTextBytes)

        Return cipherText
    End Function

    ''' <summary>
    ''' Zajistí rozkódování Xml dokumentu
    ''' </summary>
    ''' <param name="b64String">Zakódovaný dokument</param>
    ''' <param name="FullPath">Úplná cesta k souboru typu AESKEY.DEC Soubor se může jmenovat libovolně.</param>
    ''' <returns>Rozkódovaný dokument</returns>
    ''' <remarks></remarks>
    Public Function Decrypt(ByVal b64String As String, ByVal FullPath As String) As String
        'Public Function Decrypt(ByVal b64String As String, ByVal Key() As Byte) As String
        Dim aes As New System.Security.Cryptography.RijndaelManaged()
        aes.Mode = System.Security.Cryptography.CipherMode.CBC
        aes.Padding = System.Security.Cryptography.PaddingMode.None

        Dim key() As Byte
        key = GetAESKEY(FullPath)
        aes.Key = key
        Dim iv() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        aes.IV = iv
        'odstranime whitespace a prevedeme z base64
        Dim reg As New Regex("\s*")
        Dim cipherData() As Byte = System.Convert.FromBase64String(reg.Replace(b64String, ""))
        ' dekryptujeme
        Dim dec As System.Security.Cryptography.ICryptoTransform = aes.CreateDecryptor()
        Dim plainData() As Byte = dec.TransformFinalBlock(cipherData, 0, cipherData.Length)
        Dim destLen As Integer = plainData.Length
        ' najdeme konec a urizneme dodatek
        For i = plainData.Length - 1 To plainData.Length - 16 Step -1
            If plainData(i) = 10 Then
                If plainData(i - 1) = 13 Then
                    destLen = i - 1
                    Exit For
                End If
            End If
        Next
        Dim plainText As String = System.Text.Encoding.UTF8.GetString(plainData, 16, destLen - 16)
        Dim doc As XmlDocument = New XmlDocument
        doc.PreserveWhitespace = False
        doc.LoadXml(plainText)
        Console.WriteLine(Encoding.UTF8.GetString(plainData))
        Return doc.InnerXml
    End Function

    Public Function SignXmlString(ByVal Xml As String) As String
        ' vytvorime xml
        Dim doc As New XmlDocument()
        ' nechame prazdna mista jak jsou, stejne jako sifrovaci utilita
        doc.PreserveWhitespace = True
        ' nacteme dokument ze souboru
        doc.LoadXml(Xml)
        ' vytvorime hmac
        Dim HmacKey() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        ' vytvorime root element z naseho xml
        Dim root As XmlElement = doc.DocumentElement
        ' vytvorime jediny EnvelopeFooter
        Dim _NamespaceManager As XmlNamespaceManager
        _NamespaceManager = New XmlNamespaceManager(New NameTable)
        _NamespaceManager.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        Dim envFooter As XmlNode = doc.SelectSingleNode("//msg:EnvelopeFooter", _NamespaceManager)
        If (envFooter Is Nothing) Then
            Dim EnvelopeFooter As XmlElement = doc.CreateElement("msg", "EnvelopeFooter", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
            ' a celej footer element vlozime do dokumentu
            EnvelopeFooter.InnerText = ""
            root.AppendChild(EnvelopeFooter)
            envFooter = EnvelopeFooter 'InXml.SelectSingleNode("//msg:EnvelopeFooter", _NamespaceManager)
            ' vytvorime podpisovy element pro nas dokument
            ' Dim SignedXml As New SignedXml(px)
        End If
        ' vytvorime podpisovy element pro nas dokument
        Dim SignedXml As New SignedXml(doc)
        ' nastavime predepsane id podpisu
        SignedXml.Signature.Id = "identifikator-celistvosti"
        ' Specifikujeme kanonikalizacni metodu
        SignedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
        ' nastavime delku klice
        SignedXml.SignedInfo.SignatureLength = 256
        ' vytvorime keyinfo
        SignedXml.KeyInfo = New KeyInfo
        ' vytvorime element pro nazev
        Dim kn As New KeyInfoName
        ' nastavime jeho nazev
        kn.Value = "KVS HMAC"
        ' a vlozime do podpisoveho elementu
        SignedXml.KeyInfo.AddClause(kn)
        ' nastavime typ podpisu 
        Dim hm As New HMACSHA256(HmacKey)
        ' Vytvarime reference na cely xml dokument
        Dim reference As New Reference("")
        ' vyjmeme signature
        reference.AddTransform(New XmlDsigEnvelopedSignatureTransform())
        ' aplikujeme kanonikalizaci
        reference.AddTransform(New XmlDsigExcC14NWithCommentsTransform())
        ' nastavime metodu hashe
        reference.DigestMethod = EncryptedXml.XmlEncSHA256Url
        ' pridavame reference do elementu s podpisem.
        SignedXml.AddReference(reference)
        ' vytvorime podpis.
        SignedXml.ComputeSignature(hm)
        ' vlozime do nej podpis
        envFooter.AppendChild(doc.ImportNode(SignedXml.GetXml(), True))

        Return doc.InnerXml
    End Function

    ''' <summary>
    ''' Načte klíč z AESKEY
    ''' </summary>
    ''' <param name="FullPath">Úplná cesta k souboru typu AESKEY.DEC Soubor se může jmenovat libovolně.</param>
    ''' <returns>Šifrovací klíč</returns>
    ''' <remarks></remarks>
    Public Function GetAESKEY(ByVal FullPath As String) As Byte()

        Dim oFile As System.IO.FileInfo
        oFile = New System.IO.FileInfo(FullPath)

        Dim oFileStream As System.IO.FileStream = oFile.OpenRead()
        Dim lBytes As Long = oFileStream.Length

        If (lBytes > 0) Then
            Dim fileData(lBytes - 1) As Byte

            ' Read the file into a byte array
            oFileStream.Read(fileData, 0, lBytes)
            oFileStream.Close()
            Return fileData
        End If
        Return Nothing
    End Function

End Class


