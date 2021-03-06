﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SaintCoinach.Ex.Relational;

namespace Godbert.ViewModels {
    using Commands;

    public class DataViewModel : ObservableBase {
        #region Fields
        private string _SelectedSheetName;
        private IRelationalSheet _SelectedSheet;
        private DelegateCommand _ExportCsvCommand;
        #endregion

        #region Properties
        public ICommand ExportCsvCommand { get { return _ExportCsvCommand ?? (_ExportCsvCommand = new DelegateCommand(OnExportCsv)); } }
        public SaintCoinach.ARealmReversed Realm { get; private set; }

        public IEnumerable<string> AvailableSheetNames { get { return Realm.GameData.AvailableSheets; } }
        public string SelectedSheetName {
            get { return _SelectedSheetName; }
            set {
                _SelectedSheetName = value;
                _SelectedSheet = null;
                OnPropertyChanged(() => SelectedSheetName);
                OnPropertyChanged(() => SelectedSheet);
            }
        }
        public IRelationalSheet SelectedSheet {
            get {
                if (string.IsNullOrWhiteSpace(SelectedSheetName))
                    return null;
                if (_SelectedSheet == null)
                    _SelectedSheet = Realm.GameData.GetSheet(SelectedSheetName);
                return _SelectedSheet;
            }
        }
        #endregion

        #region Constructor
        public DataViewModel(SaintCoinach.ARealmReversed realm) {
            this.Realm = realm;
        }
        #endregion

        #region Export
        private void OnExportCsv() {
            if (SelectedSheet == null)
                return;

            var dlg = new Microsoft.Win32.SaveFileDialog {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = FixName(SelectedSheet.Name) + ".csv"
            };

            if (dlg.ShowDialog().GetValueOrDefault(false))
                SaveAsCsv(SelectedSheet, dlg.FileName);
        }
        private static string FixName(string original) {
            var idx = original.LastIndexOf('/');
            if (idx >= 0)
                return original.Substring(idx + 1);
            return original;
        }
        static void SaveAsCsv(IRelationalSheet sheet, string path) {
            using (var s = new StreamWriter(path, false, Encoding.UTF8)) {
                var indexLine = new StringBuilder("key");
                var nameLine = new StringBuilder("#");
                var typeLine = new StringBuilder("int32");

                var colIndices = new List<int>();
                foreach (var col in sheet.Header.Columns) {
                    indexLine.AppendFormat(",{0}", col.Index);
                    nameLine.AppendFormat(",{0}", col.Name);
                    typeLine.AppendFormat(",{0}", col.ValueType);

                    colIndices.Add(col.Index);
                }

                s.WriteLine(indexLine);
                s.WriteLine(nameLine);
                s.WriteLine(typeLine);

                foreach (var row in sheet.Cast<SaintCoinach.Ex.IRow>().OrderBy(_ => _.Key)) {
                    s.Write(row.Key);
                    foreach (var col in colIndices) {
                        var v = row[col];

                        if (v == null)
                            s.Write(",");
                        else if (IsUnescaped(v))
                            s.Write(",{0}", v);
                        else
                            s.Write(",\"{0}\"", v.ToString().Replace("\"", "\"\""));
                    }
                    s.WriteLine();
                }
            }
        }
        static bool IsUnescaped(object self) {
            return (self is Boolean
                || self is Byte
                || self is SByte
                || self is Int16
                || self is Int32
                || self is Int64
                || self is UInt16
                || self is UInt32
                || self is UInt64
                || self is Single
                || self is Double);
        }
        #endregion
    }
}
