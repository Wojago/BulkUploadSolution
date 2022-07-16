using System;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace DCMA.BulkDownLoad.Layouts.DCMA.BulkDownLoad
{
        public partial class DCMA : LayoutsPageBase
        {
        protected void Page_Load(object sender, EventArgs e)
        {
            ZipFiles();

        }

        private void ZipFiles()
        {
            string fullDocLibSourceUrl = Request.Params["sourceUrl"];
            if (string.IsNullOrEmpty(fullDocLibSourceUrl)) return;

            string listGuid = Request.Params["ListID"];


            string listName = Request.Params["ListName"];


            string docLibUrl = fullDocLibSourceUrl.Replace(SPContext.Current.Site.Url, "");

            SPList list = null;
            if (!string.IsNullOrEmpty(listGuid))
            {
                Guid listId = new Guid(listGuid);
                list = SPContext.Current.Web.Lists[listId];
            }
            else if (!string.IsNullOrEmpty(listName))
            {
                list = SPContext.Current.Web.Lists[listName];
            }

            if (list == null)
            {
                Exception ex = new Exception("Unable to get the list object");
                throw ex;

            }

            if (!list.IsDocumentLibrary()) return;

            string pItemIds = Request.Params["itemIDs"];
            if (string.IsNullOrEmpty(pItemIds)) return;

            SPDocumentLibrary library = (SPDocumentLibrary)list;

            string[] sItemIds = pItemIds.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int[] itemsIDs = new int[sItemIds.Length];
            for (int i = 0; i < sItemIds.Length; i++)
            {
                itemsIDs[i] = Convert.ToInt32(sItemIds[i]);
            }

            if (itemsIDs.Length > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (ZipFileBuilder builder = new ZipFileBuilder(ms))
                    {
                        foreach (int id in itemsIDs)
                        {
                            SPListItem item = library.GetItemById(id);
                            if (item.IsFolder())
                                AddFolder(builder, item.Folder, string.Empty);
                            else
                                AddFile(builder, item.File, string.Empty);
                        }

                        builder.Finish();
                        WriteStreamToResponse(ms);
                    }
                }
            }
        }

        private static void AddFile(ZipFileBuilder builder, SPFile file, string folder)
        {
            using (Stream fileStream = file.OpenBinaryStream())
            {
                builder.Add(folder + "\\" + file.Name, fileStream);
                fileStream.Close();
            }
        }

        private void AddFolder(ZipFileBuilder builder, SPFolder folder, string parentFolder)
        {
            string folderPath = parentFolder == string.Empty ? folder.Name : parentFolder + "\\" + folder.Name;
            builder.AddDirectory(folderPath);

            foreach (SPFile file in folder.Files)
            {
                AddFile(builder, file, folderPath);
            }

            foreach (SPFolder subFolder in folder.SubFolders)
            {
                AddFolder(builder, subFolder, folderPath);
            }
        }

        private void WriteStreamToResponse(MemoryStream ms)
        {
            if (ms.Length > 0)
            {
                string filename = DateTime.Now.ToFileTime().ToString() + ".zip";
                Response.Clear();
                Response.ClearHeaders();
                Response.ClearContent();
                Response.AddHeader("Content-Length", ms.Length.ToString());
                Response.AddHeader("Content-Disposition", "attachment; filename=" + filename);
                Response.ContentType = "application/octet-stream";

                byte[] buffer = new byte[65536];
                ms.Position = 0;
                int num;
                do
                {
                    num = ms.Read(buffer, 0, buffer.Length);
                    Response.OutputStream.Write(buffer, 0, num);
                }

                while (num > 0);

                Response.Flush();
            }
        }
    }
        public static class SPExtensions
        {
            public static bool IsFolder(this SPListItem item)
            {
                return (item.Folder != null);
            }

            public static bool IsDocumentLibrary(this SPList list)
            {
                return (list.BaseType == SPBaseType.DocumentLibrary);
            }
        }
        public class ZipFileBuilder : IDisposable
        {
            private bool disposed = false;

            ZipOutputStream zipStream = null;
            protected ZipOutputStream ZipStream
            {
                get { return zipStream; }

            }

            ZipEntryFactory factory = null;
            private ZipEntryFactory Factory
            {
                get { return factory; }
            }


            public ZipFileBuilder(Stream outStream)
            {
                zipStream = new ZipOutputStream(outStream);
                zipStream.SetLevel(9); //best compression

                factory = new ZipEntryFactory(DateTime.Now);
            }

            public void Add(string fileName, Stream fileStream)
            {
                //create a new zip entry            
                ZipEntry entry = factory.MakeFileEntry(fileName);
                entry.DateTime = DateTime.Now;
                ZipStream.PutNextEntry(entry);

                byte[] buffer = new byte[65536];

                int sourceBytes;
                do
                {
                    sourceBytes = fileStream.Read(buffer, 0, buffer.Length);
                    ZipStream.Write(buffer, 0, sourceBytes);
                }
                while (sourceBytes > 0);


            }

            public void AddDirectory(string directoryName)
            {
                ZipEntry entry = factory.MakeDirectoryEntry(directoryName);
                ZipStream.PutNextEntry(entry);
            }

            public void Finish()
            {
                if (!ZipStream.IsFinished)
                {
                    ZipStream.Finish();
                }
            }

            public void Close()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void Dispose()
            {
                this.Close();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        if (ZipStream != null)
                            ZipStream.Dispose();
                    }
                }

                disposed = true;
            }
        }

    
}

