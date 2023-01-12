using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace TSP.PackageVersionChanger
{
    class WPFOpenFileDialogService : IOpenFileDialogService
    {
        class DefaultFileInfo : IFileInfo
        {
            private readonly FileInfo _file;

            public DefaultFileInfo(FileInfo file)
            {
                if (file == null)
                    throw new ArgumentNullException(nameof(file));
                _file = file;
            }

            public long Length => _file.Length;
            public string DirectoryName => _file.DirectoryName;
            public string Name => _file.Name;
            public bool Exists => _file.Exists;
            public FileAttributes Attributes { get => _file.Attributes; set => _file.Attributes = value; }
            public StreamWriter AppendText() => _file.AppendText();
            public FileInfo CopyTo(string destinationFileName, bool overwrite) => _file.CopyTo(destinationFileName, overwrite);
            public FileStream Create() => _file.Create();
            public StreamWriter CreateText() => _file.CreateText();
            public void Delete() => _file.Delete();
            public void MoveTo(string destinationFileName) => _file.MoveTo(destinationFileName);
            public FileStream Open(FileMode mode, FileAccess access, FileShare share) => _file.Open(mode, access, share);
            public FileStream OpenRead() => _file.OpenRead();
            public StreamReader OpenText() => _file.OpenText();
            public FileStream OpenWrite() => _file.OpenWrite();

            public override string ToString() => _file.ToString();
        }

        public IFileInfo File { get; private set; }

        public IEnumerable<IFileInfo> Files => _files;
        private List<IFileInfo> _files = new List<IFileInfo>();

        private readonly Window _parent;
        private readonly OpenFileDialog _dlg;

        public WPFOpenFileDialogService(Window parent)
        {
            _parent = parent;
            _dlg = new OpenFileDialog();
        }

        public bool Multiselect { get => _dlg.Multiselect; set => _dlg.Multiselect = value; }
        public bool CheckFileExists { get => _dlg.CheckFileExists; set => _dlg.CheckFileExists = value; }
        public bool AddExtension { get => _dlg.AddExtension; set => _dlg.AddExtension = value; }
        public bool AutoUpgradeEnabled { get => false; set => throw new NotSupportedException(); }
        public bool CheckPathExists { get => _dlg.CheckPathExists; set => _dlg.CheckPathExists = value; }
        public bool DereferenceLinks { get => _dlg.DereferenceLinks; set => _dlg.DereferenceLinks = value; }
        public string InitialDirectory { get => _dlg.InitialDirectory; set => _dlg.InitialDirectory = value; }
        public bool RestoreDirectory { get => _dlg.RestoreDirectory; set => _dlg.RestoreDirectory = value; }
        public bool ShowHelp { get => false; set => throw new NotSupportedException(); }
        public bool SupportMultiDottedExtensions { get => true; set => throw new NotSupportedException(); }
        public string Title { get => _dlg.Title; set => _dlg.Title = value; }
        public bool ValidateNames { get => _dlg.ValidateNames; set => _dlg.ValidateNames = value; }
        public string Filter { get => _dlg.Filter; set => _dlg.Filter = value; }
        public int FilterIndex { get => _dlg.FilterIndex; set => _dlg.FilterIndex = value; }

        public void Reset()
        {
            _dlg.Reset();
            _files.Clear();
            File = null;
        }

        public bool ShowDialog(Action<CancelEventArgs> fileOK, string directoryName)
        {
            _files.Clear();
            File = null;
            if (_dlg.ShowDialog(_parent) == true)
            {
                if (_dlg.Multiselect)
                {
                    _files.AddRange(_dlg.FileNames.Select(f => new DefaultFileInfo(new FileInfo(f))));
                    File = _files.FirstOrDefault();
                }
                else
                {
                    File = new DefaultFileInfo(new FileInfo(_dlg.FileName));
                    _files.Add(File);
                }

                CancelEventArgs cancelArgs = new CancelEventArgs() { Cancel = false };
                fileOK?.Invoke(cancelArgs);
                if (cancelArgs.Cancel)
                    return false;

                return true;
            }
            return false;
        }
    }
}
