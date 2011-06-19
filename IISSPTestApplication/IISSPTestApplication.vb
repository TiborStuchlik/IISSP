Imports IISSPClassLibrary
Imports System.Xml
Imports System.Data.SqlClient
Imports System.Threading
Imports System.IO
Imports System.IO.Stream
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Text.RegularExpressions


Public Class IISSPTestApplication

    Private trd As Thread
    Dim Settings As XmlDocument
    Dim doc As XmlDataDocument = New XmlDataDocument
    Dim Err As XmlDataDocument = New XmlDataDocument
    Dim detail As XmlDataDocument = New XmlDataDocument
    Dim fm As IISSPROP = New IISSPROP
    Dim bf As Boolean = False
    Dim ef As Boolean = False
    Dim bdf As Boolean = False
    Dim Errf As Boolean = False
    Dim RZHS As String = "0"
    Dim RZDVO As String = ""
    Dim RZDVD As String = ""
    Dim RZS As String = ""
    Dim RTDZ As String = ""
    Dim Res As String = ""
    Dim GUserName As String = ""
    Dim GPassword As String = ""
    Dim GUrl As String = ""
    Dim GModule As String = ""
    'deklarujeme tridu inbox pro praci s inboxem
    Dim Inbox As IISSPInbox = New IISSPInbox

    Private Sub Log(ByVal text As String)
        ' RBLog.Text += Date.Now.ToString("yyyy.MM.dd hh:mm:ss ") + text + vbCrLf
        RBLog.SelectionColor = Color.Red
        RBLog.AppendText(Date.Now.ToString("yyyy.MM.dd hh:mm:ss "))
        RBLog.SelectionColor = Color.Black
        RBLog.AppendText(text + vbCrLf)
        RBLog.SelectionStart = RBLog.TextLength
        RBLog.ScrollToCaret()
    End Sub

    Private Sub IISSPTestApplication_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Log("Startuji aplikaci.")
        RBLog.ZoomFactor = 1.0F
        LAppName.Text = "IISSP Testovací aplikace, v. " + My.Application.Info.Version.ToString
        ComboBox1.SelectedIndex = 0
        PMsg.Dock = DockStyle.Fill
        PProgress.Dock = DockStyle.Fill
        PErr.Dock = DockStyle.Fill
        PDetailMain.Dock = DockStyle.Fill
        Application.DoEvents()
        TBDetail.DataBindings.Add("Text", BindingSource2, "ZaznamText")
        LDetailTime.DataBindings.Add("Text", BindingSource2, "Timestamp")
        LEPopis.DataBindings.Add("Text", BindingSource3, "Popis")
        LENumber.DataBindings.Add("Text", BindingSource3, "Number")
        LENazev.DataBindings.Add("Text", BindingSource3, "Nazev")
        cmbCiselnik.SelectedIndex = 0

        Settings = New XmlDocument
        Settings.Load("settings.xml")
        For Each n As Xml.XmlNode In Settings.SelectNodes("/Tiba/services/service")
            CBRequests.Items.Add(n.Attributes("name").Value)
        Next

        ' nastavime obecne vlastnosti pro dotazy
        Inbox.General.Loging = True
        Inbox.General.TimeOut = 10000
        Inbox.General.SenderResponsiblePersonEmail = "benes@insyco.cz"
        Inbox.General.SenderResponsiblePersonId = "EU1620000273"
        Inbox.General.SenderResponsiblePersonName = "Beneš Jiří"
        Inbox.General.SenderIc = "00164801"
        Inbox.General.SenderSubjectName = "IN-SY-CO"
        Inbox.General.RecipientIc = "00006947"
        Inbox.General.RecipientSubjectName = "Ministerstvo financí"
        Inbox.General.WorkingDirectory = Application.StartupPath + "\"

        CBRequests.SelectedIndex = 0
        CBInbox.SelectedIndex = 0
        ' Log("Spouštím request")
        ' BackgroundWorker1.RunWorkerAsync()

    End Sub

    Private Sub HideAllPanel()
        PErr.Visible = False
        PMsg.Visible = False
        PProgress.Visible = False
    End Sub

    Private Sub ClearXmlDataDocument(ByVal x As XmlDataDocument)
        For Each tb As DataTable In x.DataSet.Tables
            For i As Integer = 0 To tb.Rows.Count - 1
                tb.Rows.RemoveAt(0)
            Next
        Next
    End Sub

    Private Sub BindGrid()
        Dim RXml As XmlDocument = New XmlDocument
        RXml.LoadXml(Res)
        Dim xr As XmlTextReader = New XmlTextReader(New StringReader(RXml.OuterXml))
        HideAllPanel()

        If RXml.DocumentElement.Name = "Error" Then
            Errf = True
            ClearXmlDataDocument(Err)
            Err.DataSet.ReadXml(xr)
            ErrorDs = Err.DataSet
            BindingSource3.DataSource = ErrorDs
            BindingSource3.DataMember = "Error"
            PErr.Visible = True
        Else
            Errf = False
            ClearXmlDataDocument(doc)
            doc.DataSet.ReadXml(xr)
            IISSPDataSet = doc.DataSet
            BindingSource1.DataSource = IISSPDataSet.DefaultViewManager
            BindingSource1.DataMember = "zprava"
            PMsg.Visible = True
        End If

    End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Res = Inbox.GetMessagesHeaders(RZHS, RZDVO, RZDVD, RZS, RTDZ)
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        Log("Dokončeno s výsledkem:")
        Log(Res)
        BindGrid()
    End Sub

    Private Sub ToolStripButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton1.Click, Button9.Click
        RZHS = "0"
        RZDVO = ""
        RZDVD = ""
        RZS = ""
        RTDZ = ""
        HideAllPanel()
        PProgress.Visible = True
        Log("Spouštím request")
        BackgroundWorker1.RunWorkerAsync()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Log("Načítám detail zprávy s ID: " + Label5.Text)
        Dim mc As IISSPInbox = New IISSPInbox
        mc.General.Loging = True
        Dim s As String = mc.GetMessageById(Label5.Text)
        Dim xr As XmlTextReader = New XmlTextReader(New StringReader(s))
        Log(s)
        ClearXmlDataDocument(detail)
        detail.DataSet.ReadXml(xr)
        DetailDs = detail.DataSet
        For Each x As DataTable In DetailDs.Tables
            ' s = s + x.TableName + " - "
        Next
        BindingSource2.DataSource = DetailDs.DefaultViewManager
        BindingSource2.DataMember = "DetailZpracovani"
        DRDetail.DataSource = BindingSource2
        If bdf Then
            TBDetail.DataBindings.Add("Text", BindingSource2, "ZaznamText")
            LDetailTime.DataBindings.Add("Text", BindingSource2, "Timestamp")
            bdf = False
        End If

    End Sub

    Private Sub DataRepeater1_DrawItem(ByVal sender As Object, ByVal e As Microsoft.VisualBasic.PowerPacks.DataRepeaterItemEventArgs) Handles DRDetail.DrawItem

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        If ComboBox1.SelectedIndex = 0 Then
            RZS = ""
        ElseIf ComboBox1.SelectedIndex = 1 Then
            RZS = "N"
        ElseIf ComboBox1.SelectedIndex = 2 Then
            RZS = "R"
        Else
            RZS = "D"
        End If
        RTDZ = TBTyp.Text
        If ChBHz.Checked Then
            RZHS = "1"
        Else
            RZHS = "0"
        End If
        RZDVO = DateTimePicker1.Value.ToString("yyyy-MM-dd")
        RZDVD = DateTimePicker2.Value.ToString("yyyy-MM-dd")
        HideAllPanel()
        PProgress.Visible = True
        Log("Spouštím request")
        BackgroundWorker1.RunWorkerAsync()
    End Sub

    Private Sub BindingSource1_PositionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles BindingSource1.PositionChanged
        Button1.Visible = False
        Dim ds As DataTable = IISSPDataSet.Tables("zprava")
        If ds.Rows.Count < 1 Then
            Return
        End If
        If Me.BindingSource1.Current Is Nothing Then
        Else
            Dim row As DataRowView
            row = CType(Me.BindingSource1.Current, DataRowView)
            pok.Text = row.Item(0)
            Label5.Text = row.Item(0)
            Label6.Text = row.Item(1)
            Label7.Text = row.Item(3)
            Label8.Text = row.Item(4)
            Label9.Text = row.Item(5)


            Button1.Text = "Načti zprávu s ID: " + Label5.Text
            Button1.Visible = True

        End If
    End Sub

    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar1.Scroll
        RBLog.ZoomFactor = 1.0F + (TrackBar1.Value / 100)
    End Sub

    Private Sub CBRequests_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CBRequests.SelectedIndexChanged
        ' zmena combobox
        With Settings.SelectNodes("/Tiba/services/service")(CBRequests.SelectedIndex)

            LRozhraniContext.Text = .SelectSingleNode("Description").InnerXml
            GUserName = .SelectSingleNode("UserName").InnerText
            GPassword = .SelectSingleNode("Password").InnerText
            LRozhraniTestUrl.Text = "TestUrl: " + .SelectSingleNode("TestUrl").InnerXml
            GUrl = .SelectSingleNode("TestUrl").InnerText
            GModule = .SelectSingleNode("Module").InnerText
            CBRozhraniDotaz.Items.Clear()
            CBRozhraniDotaz.Text = "neni příklad"
            TBSource.Text = ""
            For Each n As XmlNode In .SelectNodes("Requests/Request[@name]")
                CBRozhraniDotaz.Items.Add(n.Attributes("name").Value)
            Next
            If CBRozhraniDotaz.Items.Count > 0 Then
                CBRozhraniDotaz.SelectedIndex = 0
            End If
        End With
    End Sub

    Private Sub CBRozhraniDotaz_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CBRozhraniDotaz.SelectedIndexChanged
        Dim N As XmlNode = Settings.SelectNodes("/Tiba/services/service")(CBRequests.SelectedIndex)
        Dim X As XmlNode = N.SelectNodes("Requests/Request")(CBRozhraniDotaz.SelectedIndex)
        Dim ms As MemoryStream = New MemoryStream()
        Dim XW As XmlTextWriter = New XmlTextWriter(ms, Encoding.UTF8)
        XW.WriteStartDocument()
        XW.Formatting = Formatting.Indented
        X.WriteContentTo(XW)
        XW.WriteEndDocument()
        XW.Flush()
        ms.Seek(0, SeekOrigin.Begin)
        Dim sr As StreamReader = New StreamReader(ms)
        TBSource.Text = sr.ReadToEnd
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        WebBrowser1.Navigate(My.Application.Info.DirectoryPath + "\progress.htm")
        My.Application.DoEvents()
        ' Nastavovací varianta requestu implementována v třídě general
        ' Nejprve je potřeba nastavit proměné
        ' pokud se neshodují s posledně nastavenými (uchovávajíse v settings aplikace)
        Dim IG As IISSPGeneral = New IISSPGeneral
        IG.Loging = True
        IG.MyRequest = TBSource.Text
        IG.UserName = GUserName
        IG.Password = GPassword
        IG.Url = GUrl
        IG.TimeOut = 15000
        IG.RecipientModule = GModule
        IG.SenderResponsiblePersonEmail = "benes@insyco.cz"
        IG.SenderResponsiblePersonId = "EU1620000273"
        IG.SenderResponsiblePersonName = "Beneš Jiří"
        IG.SenderIc = "00164801"
        IG.SenderSubjectName = "IN-SY-CO"
        IG.RecipientIc = "00006947"
        IG.RecipientSubjectName = "Ministerstvo financí"

        ' zatim se automaticky nacita pri inicializaci General z resource
        ' IG.ClientCertificate = New X509Certificate2("tiba.pfx", "tiba")
        Dim OutXml As New XmlDocument
        OutXml.LoadXml(IG.Request())
        OutXml.Save("output.xml")
        WebBrowser1.Navigate(My.Application.Info.DirectoryPath + "\output.xml")
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        WebBrowser2.Navigate(My.Application.Info.DirectoryPath + "\progress.htm")
        My.Application.DoEvents()
        Dim doc As XmlDocument = New XmlDocument
        'Dim aa As String
        'aa = cmbCiselnik.Text & "," & TBKapitola.Text & "," & DT1.Value.ToString("yyyy-MM-dd") & "," & DT2.Value.ToString("yyyy-MM-dd") & "," & TB1.Text & "," & NUD1.Value & "," & TB2.Text & "," & TB3.Text & "," & TB4.Text & "," & NUP2.Value & "," & DT3.Value.ToString("s") + "Z" & "," & DT4.Value.ToString("s") + "Z"
        doc.LoadXml(fm.GetFMMD_Ciselnik(cmbCiselnik.Text, TBKapitola.Text, DT1.Value.ToString("yyyy-MM-dd"), DT2.Value.ToString("yyyy-MM-dd"), TB1.Text, NUD1.Value, TB2.Text, TB3.Text, TB4.Text, NUP2.Value, DT3.Value.ToString("s") + "Z", DT4.Value.ToString("s") + "Z", TBRokFiskalni.Text))
        'doc.LoadXml(fm.GetFMMD_Ciselnik(cmbCiselnik.Text, TBKapitola.Text, "", "", "", "", "", "", "", "", "", "", TBRokFiskalni.Text))
        doc.Save("output2.xml")
        WebBrowser2.Navigate(My.Application.Info.DirectoryPath + "\output2.xml")
    End Sub

    Private Sub LinkLabel1_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        System.Diagnostics.Process.Start("http://iissp.stuchlik.info")
    End Sub

    Private Sub cbDotaz_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbRopDotaz.SelectedIndexChanged

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        WebBrowser2.Navigate(My.Application.Info.DirectoryPath + "\progress.htm")
        My.Application.DoEvents()
        Dim doc As XmlDocument = New XmlDocument

        'doc.LoadXml(fm.GetFMMD_Ciselnik(cmbCiselnik.Text, TBKapitola.Text, DT1.Value.ToString("yyyy-MM-dd"), DT2.Value.ToString("yyyy-MM-dd"), TB1.Text, NUD1.Value, TB2.Text, TB3.Text, TB4.Text, NUP2.Value, DT3.Value.ToString("s") + "Z", DT4.Value.ToString("s") + "Z", TBRokFiskalni.Text))

        If cbRopDotaz.Text = "Url_SP_EKIS_ROP" Then
            doc.LoadXml(fm.GetSP_EKIS_ROP(txKapitola.Text, txRozpOpRok.Text, txRozpOpDotaz.Text))
        End If

        If cbRopDotaz.Text = "Url_EKIS_SP_ROP" Then
            doc.LoadXml(fm.GetEKIS_SP_ROP(Me.txtROP_Request.Text))
        End If



        doc.Save("output2.xml")
        WebBrowserROP.Navigate(My.Application.Info.DirectoryPath + "\output2.xml")
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Dim Xml As XmlDocument = New XmlDocument
        Xml.PreserveWhitespace = True
        Xml.LoadXml(TBSignedSource.Text)
        Dim IG As IISSPGeneral = New IISSPGeneral
        IG.SetClientCertificate("settings\tiba.pfx", "tiba")
        Xml = IG.SignXml(Xml)
        Xml = IG.FormatXml(Xml)
        TBSignedSource.Text = Xml.OuterXml
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        System.Diagnostics.Process.Start("IISSPClassLibraryDokumentace.chm")
    End Sub

    Private Sub CBInbox_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles CBInbox.SelectedIndexChanged

        If CBInbox.SelectedIndex = 0 Then
            ' nastavujeme udaje pro RISRE
            Inbox.General.Url = "https://portal5.statnipokladna.cz/csuis/wstest/inbox"
            Inbox.General.UserName = "2000000002"
            Inbox.General.Password = "lr3zr6c5"
            Inbox.General.RecipientModule = "CSUIS"

            ' nastavujeme udaje pro CSUIS
        ElseIf CBInbox.SelectedIndex = 1 Then
            Inbox.General.Url = "https://testportal3.statnipokladna.cz/risre/ws/INBOX"
            Inbox.General.RecipientModule = "RISRE"
        End If

        HideAllPanel()
        Button1.Visible = False
        PProgress.Visible = True
        Log("Spouštím request " + Inbox.General.RecipientModule)
        'BackgroundWorker1.RunWorkerAsync()
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        Dim doc As XmlDocument = New XmlDocument
        doc.LoadXml(TBTest.Text)
        Dim nsmgr As XmlNamespaceManager = New XmlNamespaceManager(doc.NameTable)
        nsmgr.AddNamespace("SOAP", "http://schemas.xmlsoap.org/soap/envelope/")
        nsmgr.AddNamespace("msg", "urn:cz:mfcr:iissp:schemas:Messaging:v1")
        nsmgr.AddNamespace("cmn", "urn:cz:mfcr:iissp:schemas:Common:v1")
        Try
            Dim node As XmlNode = doc.SelectSingleNode(TBXpath.Text, nsmgr)
            If node Is Nothing Then
                LTest.Text = "nenalezeno"
            Else
                LTest.Text = "OK"
            End If
        Catch
            LTest.Text = "error"
        End Try
    End Sub

   
    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        Dim Crypto As IISSPCrypto = New IISSPCrypto
        Try
            TBCOutput.Text = Crypto.Decrypt(TBCInput.Text, Application.StartupPath + "\AESKEY.DEC")
        Catch
            MsgBox("Dešifrování selhalo.", 1)
        End Try

    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        Dim Crypto As IISSPCrypto = New IISSPCrypto
        Try
            Dim FullPath = Application.StartupPath

            TBCOutput.Text = Crypto.Encrypt(TBCInput.Text, FullPath + "\AESKEY.DEC")
        Catch
            MsgBox("Šifrování selhalo.", 1)
        End Try
    End Sub

    Private Sub RBLog_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RBLog.TextChanged

    End Sub

    Private Sub TabControl1_Selecting(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TabControlCancelEventArgs) Handles TabControl1.Selecting

    End Sub


    Private Sub TabControl1_Selected(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TabControlEventArgs) Handles TabControl1.Selected
        If e.TabPage.Text = "Protokol IISSPClassLibrary" Then
            Me.LogTB.Text = My.Computer.FileSystem.ReadAllText(Inbox.General.WorkingDirectory + "log\log.txt")
            Me.LogTB.SelectionStart = Me.LogTB.Text.Length - 10
            Me.LogTB.SelectionLength = 5
            'LogTB.SelectAll()

            LogTB.Focus()
            Me.LogTB.ScrollToCaret()
            'LogTB.SelectAll()

        End If
    End Sub
End Class
