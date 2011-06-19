// http://aspnetupload.com
// Copyright © 2009 Krystalware, Inc.
//
// This work is licensed under a Creative Commons Attribution-Share Alike 3.0 United States License
// http://creativecommons.org/licenses/by-sa/3.0/us/

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.IO;

namespace Krystalware.UploadHelper
{
    public class HttpUploadHelper
    {
        private HttpUploadHelper()
        { }

        public static string Upload(string url, UploadFile[] files, NameValueCollection form)
        {
            HttpWebResponse resp = Upload((HttpWebRequest)WebRequest.Create(url), files, form);

            using (Stream s = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(s))
            {
                return sr.ReadToEnd();
            }
        }

        public static HttpWebResponse Upload(HttpWebRequest req, UploadFile[] files, NameValueCollection form)
        {
            List<MimePart> mimeParts = new List<MimePart>();

            try
            {
                foreach (string key in form.AllKeys)
                {
                    StringMimePart part = new StringMimePart();

                    part.Headers["Content-Disposition"] = "form-data; name=\"" + key + "\"";
                    part.StringData = form[key];

                    mimeParts.Add(part);
                }

                int nameIndex = 0;

                foreach (UploadFile file in files)
                {
                    StreamMimePart part = new StreamMimePart();

                    if (string.IsNullOrEmpty(file.FieldName))
                        file.FieldName = "file" + nameIndex++;

                    //part.Headers["Content-Disposition"] = "form-data; name=\"" + file.FieldName + "\"; filename=\"" + file.FileName + "\"";
                    part.Headers["Content-Type"] = file.ContentType;
                    //part.Headers["Content-Transfer-Encoding"] = "8bit";
                    if (nameIndex < 1)
                    {
                        part.Headers["Content-ID"] = "<rootpart@soapui.org>";
                        //part.Headers["Content-Transfer-Encoding"] = "base64";
                    }

                    part.SetStream(file.Data);

                    mimeParts.Add(part);
                }

                //string boundary = "----=_Part_0_" + DateTime.Now.Ticks.ToString("x");
                string boundary = "----=_Part_0_2535725.1277885346844";
                
                // req.ContentType = "multipart/form-data; boundary=" + boundary;
                req.ContentType = "Multipart/Related; type=\"text/xml\"; charset=utf-8; boundary=\"" + boundary + "\"; start=\"<rootpart@soapui.org>\"";
                //req.ContentType = "multipart/related; boundary=\"" + boundary + "\"";
                req.Method = "POST";
                req.Headers["MIME-Version"] = "1.0";

                long contentLength = 0;

                byte[] _footer = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");

                foreach (MimePart part in mimeParts)
                {
                    contentLength += part.GenerateHeaderFooterData(boundary);
                }

                byte[] buffer = new byte[8192];
                byte[] afterFile = Encoding.UTF8.GetBytes("\r\n");
                int read;

                req.ContentLength = contentLength + _footer.Length + afterFile.Length;
                //req.ContentLength = 3326;
                
                using (Stream s = req.GetRequestStream())
                {
                    s.Write(afterFile, 0, afterFile.Length);
                    foreach (MimePart part in mimeParts)
                    {
                        s.Write(part.Header, 0, part.Header.Length);

                        while ((read = part.Data.Read(buffer, 0, buffer.Length)) > 0)
                            s.Write(buffer, 0, read);

                        part.Data.Dispose();

                        s.Write(afterFile, 0, afterFile.Length);
                    }

                    s.Write(_footer, 0, _footer.Length);
                }

                return (HttpWebResponse)req.GetResponse();
            }
            catch
            {
                foreach (MimePart part in mimeParts)
                    if (part.Data != null)
                        part.Data.Dispose();

                throw;
            }
        }
    }
}