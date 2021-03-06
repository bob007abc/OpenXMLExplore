﻿using System;
using System.Windows.Forms;
using SharpShell.SharpContextMenu;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using SharpShell.Attributes;
using System.Collections.Generic;
using System.Xml;
using System.IO.Compression;

namespace OpenXMLExplorer
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".xlsx", ".docx", ".pptx")]
    public class OpenXMLExtract : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //  Create the menu strip.
            var menu = new ContextMenuStrip();

            //  Create a menu item.
            var openxmlExtract = new ToolStripMenuItem("OpenXML Extract");
            openxmlExtract.Click += (sender, args) => DoExtract(sender, args);

            //  Add the item to the context menu.
            menu.Items.Add(openxmlExtract);

            //  Return the menu.
            return menu;
        }


        private void DoExtract(object sender, EventArgs args)
        {
            //  Go through each file.
            foreach (var filePath in SelectedItemPaths)
            {
                this.Extract(filePath);
            }

            //  Show the ouput.
            //MessageBox.Show("Extract success!");
        }

        private void Extract(string filePath)
        {
            try
            {
                //Begin extract
                string text = Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filePath);
                if (Directory.Exists(text))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(text);
                    directoryInfo.Delete(true);
                }

                //Empty file
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    Directory.CreateDirectory(text);
                    return;
                }

                string text2 = Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar + Path.GetRandomFileName();
                File.Copy(filePath, text2);

                //Begin Extract to directory
                ZipFile.ExtractToDirectory(text2, text);
                File.SetAttributes(text2, FileAttributes.Normal);
                File.Delete(text2);

                //Begin Formatting xml
                this.FormateXmlFiles(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.ReadKey();
            }
        }

        private void FormateXmlFiles(string directory)
        {
            DirectoryInfo directoryInfo;
            try
            {
                directoryInfo = new DirectoryInfo(directory);
            }
            catch (Exception)
            {
                Console.WriteLine("Directory is not exist : " + directory);
                return;
            }

            if (directoryInfo.Exists)
            {
                List<string> list = this.ListFiles(directoryInfo);
                foreach (string current in list)
                {
                    try
                    {
                        Console.WriteLine("--Formatting file begin--: " + current);
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.Load(current);
                        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(current, Encoding.UTF8))
                        {
                            xmlTextWriter.Indentation = 4;
                            xmlTextWriter.Formatting = Formatting.Indented;
                            xmlDocument.WriteContentTo(xmlTextWriter);
                        }
                        Console.WriteLine("--Formatting file successed--");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("--Format file error : " + ex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Directory is not exist : " + directory);
            }
        }

        private List<string> ListFiles(FileSystemInfo info)
        {
            List<string> list = new List<string>();
            if (!info.Exists)
            {
                return list;
            }

            DirectoryInfo directoryInfo = info as DirectoryInfo;
            if (directoryInfo == null)
            {
                return list;
            }

            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
            for (int i = 0; i < fileSystemInfos.Length; i++)
            {
                FileInfo fileInfo = fileSystemInfos[i] as FileInfo;
                if (fileInfo != null)
                {
                    if ((fileInfo.Extension.ToLower().Equals(".xml") || fileInfo.Extension.ToLower().Equals(".rels")) && fileInfo.Length < 20971520L)
                    {
                        list.Add(fileInfo.FullName);
                    }
                }
                else
                {
                    list.AddRange(this.ListFiles(fileSystemInfos[i]));
                }
            }

            return list;
        }
    }

    [ComVisible(true)]
    [COMServerAssociation(AssociationType.Directory)]
    public class OpenXMLCompress : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //  Create the menu strip.
            var menu = new ContextMenuStrip();

            //  Create a menu item.
            var openxmlCompress = new ToolStripMenuItem("OpenXML Compress");
            var excelItem = openxmlCompress.DropDownItems.Add(DocumentType.Excel.GetFileExtension());
            var wordItem = openxmlCompress.DropDownItems.Add(DocumentType.Word.GetFileExtension());
            var pptItem = openxmlCompress.DropDownItems.Add(DocumentType.PowerPoint.GetFileExtension());

            //Add events
            excelItem.Click += (sender, args) => DoCompress(sender, args, DocumentType.Excel);
            wordItem.Click += (sender, args) => DoCompress(sender, args, DocumentType.Word);
            pptItem.Click += (sender, args) => DoCompress(sender, args, DocumentType.PowerPoint);

            //  Add the item to the context menu.
            menu.Items.Add(openxmlCompress);

            //  Return the menu.
            return menu;
        }


        private void DoCompress(object sender, EventArgs args, DocumentType dType)
        {
            //  Go through each file.
            foreach (var filePath in SelectedItemPaths)
            {
                this.Compress(filePath, dType);
            }
        }

        private void Compress(string filePath, DocumentType dType)
        {
            try
            {
                //Console.WriteLine("Begin compress ... ");
                string fileName = Path.GetFileName(filePath);
                DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                string fullName = directoryInfo.Parent.FullName;
                string text = string.Concat(new object[]
                {
                    fullName,
                    Path.DirectorySeparatorChar,
                    fileName,
                    dType.GetFileExtension()
                });

                int num = 1;
                while (File.Exists(text))
                {
                    text = string.Concat(new object[]
                    {
                        fullName,
                        Path.DirectorySeparatorChar,
                        fileName,
                        num++,
                        dType.GetFileExtension()
                    });

                    if (num > 1000)
                    {
                        throw new Exception("Too many duplicate file[" + fileName + "] with numeric suffix.");
                    }
                }

                ZipFile.CreateFromDirectory(filePath, text);
                //Console.WriteLine("Success compress ... ");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.ReadKey();
            }
        }
    }

    public enum DocumentType
    {
        Excel,
        Word,
        PowerPoint
    }

    public static class Helpers
    {
        public static string GetFileExtension(this DocumentType dType)
        {
            string fe = string.Empty;

            switch (dType)
            {
                case DocumentType.Excel:
                    fe = ".xlsx";
                    break;
                case DocumentType.Word:
                    fe = ".docx";
                    break;
                case DocumentType.PowerPoint:
                    fe = ".pptx";
                    break;
                default:
                    break;
            }

            return fe;
        }
    }
}
